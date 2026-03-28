using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Role.Organizer;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using AEMS_Solution.Models.Organizer;
using AEMS_Solution.Models.Organizer.Manage;
using AutoMapper;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Event.Sub_Service.Location;
using BusinessLogic.Service.Event.Sub_Service.Feedback;
using BusinessLogic.Service.Organizer;
using BusinessLogic.Service.UserActivities;
using BusinessLogic.Service.ValidationData.Event;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : BaseController
    {
        private readonly IOrganizerService _organizerService;
        private readonly IMapper _mapper;
        private readonly ILocationService _locationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventService _eventService;
        private readonly BusinessLogic.Storage.IFileStorageService _fileStorageService;
        private readonly IFeedBackService _feedbackService;

        public OrganizerController(
            IOrganizerService organizerService,
            IEventService eventService,
            IMapper mapper,
            ILocationService locationService,
            IUnitOfWork unitOfWork,
            BusinessLogic.Storage.IFileStorageService fileStorageService,
            IFeedBackService feedbackService)
        {
            _organizerService = organizerService;
            _eventService = eventService;
            _mapper = mapper;
            _locationService = locationService;
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _feedbackService = feedbackService;
        }

        [HttpGet]
        public async Task<IActionResult> Manage(string? operation, string? legacyAction, string? id, string? search = null, string? status = null, string? semesterId = null, int page = 1, int pageSize = 10)
        {
            var op = (operation ?? legacyAction)?.Trim();
            if (string.IsNullOrWhiteSpace(op)) return RedirectToAction("Index");

            try
            {
                switch (op.ToLowerInvariant())
                {
                    case "create":
                        ModelState.Clear();
                        return await Create();

                    case "participants":
                        return await Participants(id);

                    case "detailevent":
                    case "detail":
                        return await DetailEvent(id);

                    case "myevents":
                        {
                            EventStatusEnum? parsedStatus = null;
                            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatusEnum>(status, true, out var tmp))
                            {
                                parsedStatus = tmp;
                            }
                            return await MyEvents(search, parsedStatus, semesterId, page, pageSize);
                        }

                    case "myeventsdelete":
                        {
                            EventStatusEnum? parsedStatus = null;
                            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatusEnum>(status, true, out var tmp))
                            {
                                parsedStatus = tmp;
                            }
                            return await MyEventsDelete(search, parsedStatus, semesterId, page, pageSize);
                        }

                    case "manageticket":
                        return await ManageTicket(search);

                    case "ticketsbyevent":
                        return await TicketsByEvent(id, search, status);

                    case "reanalyze-sentiment":
                        return await ReAnalyzeSentiment(id);

                    case "viewfeedback":
                        return await ViewFeedback(id);

                    default:
                        return BadRequest("Operation không hợp lệ.");
                }
            }
            catch (Exception)
            {
                SetError("Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại.");
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ManageTicket(string? search)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var events = await _organizerService.GetMyEventsAsync(userId);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    events = events.Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var model = new ManageTicketViewModel
                {
                    Search = search,
                    Events = events
                        .OrderByDescending(x => x.StartTime)
                        .Select(x => new TicketSalesByEventVm
                        {
                            EventId = x.EventId,
                            EventTitle = x.Title,
                            StartTime = x.StartTime,
                            EndTime = x.EndTime,
                            MaxCapacity = x.MaxCapacity,
                            SoldTickets = x.RegisteredCount
                        })
                        .ToList()
                };

                return View("~/Views/Ticket/ManageTicket.cshtml", model);
            }
            catch (Exception)
            {
                SetError("Đã xảy ra lỗi khi tải thống kê vé.");
                return View("~/Views/Ticket/ManageTicket.cshtml", new ManageTicketViewModel());
            }
        }

        // ── TicketsByEvent: drill-down — individual tickets for one event ─────────
        /// <summary>
        /// Route: GET /Organizer/TicketsByEvent?eventId=xxx&amp;search=yyy&amp;status=zzz
        /// Shows all ticket records for a single event.
        /// Team TODO: replace _unitOfWork.Tickets stub with a proper ITicketService call.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TicketsByEvent(string? eventId, string? search, string? status)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return NotFound();
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                // ── Load event summary ──────────────────────────────────────
                var myEvents = await _organizerService.GetMyEventsAsync(userId);
                var evt = myEvents.FirstOrDefault(e => e.EventId == eventId);
                if (evt == null)
                {
                    SetError("Sự kiện không tồn tại hoặc bạn không có quyền truy cập.");
                    return RedirectToAction(nameof(ManageTicket));
                }

                // ── Load tickets via UnitOfWork ─────────────────────────────
                // Team TODO: swap this line for a proper service injection:
                // var tickets = await _ticketService.GetByEventAsync(eventId);
                var allTickets = await _unitOfWork.Tickets.GetAllAsync(
                    t => t.EventId == eventId && t.DeletedAt == null,
                    q => q.Include(t => t.Student).ThenInclude(s => s.User));

                // ── Search filter (student name or ticket code) ─────────────
                if (!string.IsNullOrWhiteSpace(search))
                {
                    allTickets = allTickets.Where(t =>
                        (t.Student.User.FullName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (t.TicketCode ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // ── Status filter ───────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<TicketStatusEnum>(status, true, out var parsedStatus))
                {
                    allTickets = allTickets.Where(t => t.Status == parsedStatus).ToList();
                }

                var ticketVms = allTickets
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new TicketListItemVm
                    {
                        TicketId    = t.Id,
                        EventId     = t.EventId,
                        StudentId   = t.StudentId,
                        EventName   = evt.Title,
                        TicketCode  = t.TicketCode,
                        Status      = t.Status,
                        CheckInTime = t.CheckInTime,
                        StudentName = t.Student?.User?.FullName ?? t.StudentId
                    }).ToList();

                var vm = new TicketsByEventViewModel
                {
                    EventId      = eventId,
                    EventTitle   = evt.Title,
                    StartTime    = evt.StartTime,
                    EndTime      = evt.EndTime,
                    MaxCapacity  = evt.MaxCapacity,
                    Search       = search,
                    StatusFilter = status,
                    Tickets      = ticketVms
                };

                return View("~/Views/Ticket/TicketsByEvent.cshtml", vm);
            }
            catch (Exception ex)
            {
                SetError($"Lỗi khi tải danh sách vé: {ex.Message}");
                return RedirectToAction(nameof(ManageTicket));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReAnalyzeSentiment(string id)
        {
            if (CurrentUserId == null) return Unauthorized();
            if (string.IsNullOrEmpty(id)) return BadRequest("Event ID is required.");

            try
            {
                // Verify ownership
                var ev = await _unitOfWork.Events.GetAsync(e => e.Id == id && e.DeletedAt == null);
                if (ev == null) return NotFound("Event not found.");
                if (ev.OrganizerId != GetOrganizerProfileId()) return Forbid();

                int count = await _feedbackService.AnalyzeEventFeedbacksAsync(id);
                await ExecuteSuccessAsync($"Đã đồng bộ và phân tích xong {count} đánh giá.", UserActionType.Sync, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, $"Lỗi khi đồng bộ cảm xúc: {ex.Message}");
            }

            return RedirectToAction("Manage", new { operation = "myevents" });
        }

        private string GetOrganizerProfileId()
        {
             var profile = _unitOfWork.StaffProfiles.GetAsync(sp => sp.UserId == CurrentUserId).Result;
             return profile?.Id ?? string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> UploadThumbnail(string id, IFormFile file)
        {
            if (CurrentUserId == null) return Unauthorized();
            if (string.IsNullOrEmpty(id)) return BadRequest("Event Id không hợp lệ.");
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn ảnh.");

            try
            {
                var newUrl = await _organizerService.UpdateThumbnailAsync(id, file, CurrentUserId);
                if (newUrl != null)
                {
                    return Json(new { success = true, url = newUrl });
                }
                return Json(new { success = false, message = "Không thể tải ảnh lên." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEventImage(string eventId, IFormFile file)
        {
            if (string.IsNullOrEmpty(eventId)) return Json(new { success = false, message = "Event ID không hợp lệ." });
            if (file == null || file.Length == 0) return Json(new { success = false, message = "Vui lòng chọn ảnh." });

            try
            {
                var url = await _organizerService.AddEventImageAsync(eventId, file, CurrentUserId);
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEventImage(string eventId, string imageUrl)
        {
            if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(imageUrl)) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            try
            {
                await _organizerService.RemoveEventImageAsync(eventId, imageUrl, CurrentUserId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(string? operation, string? legacyAction, CreateEventViewModel vm, string? id)
        {
            var op = (operation ?? legacyAction)?.Trim();
            if (string.IsNullOrWhiteSpace(op)) return RedirectToAction("Index");

            try
            {
                switch (op.ToLowerInvariant())
                {
                    case "create":
                        return await Create(vm);

                    case "sendforapproval":
                    case "send":
                        return await SendForApproval(id);

                    case "publish":
                        return await PublishEvent(id);

                    case "cancel":
                        return await CancelEvent(id);

                    case "softdelete":
                        if (string.IsNullOrEmpty(id))
                        {
                            SetError("Event id không hợp lệ.");
                            return RedirectToAction("MyEvents");
                        }
                        var userId = CurrentUserId;
                        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
                        try
                        {
                            await _organizerService.SoftDeleteEventAsync(userId, id);
                            await ExecuteSuccessAsync("Xóa sự kiện thành công.", UserActionType.SoftDelete, id, TargetType.Event);
                        }
                        catch (Exception ex)
                        {
                            await ExecuteErrorAsync(ex, "Đã xảy ra lỗi khi xóa sự kiện.");
                        }
                        return RedirectToAction("MyEvents");

                    case "restore":
                        if (string.IsNullOrEmpty(id))
                        {
                            SetError("Event id không hợp lệ.");
                            return RedirectToAction("MyEventsDelete");
                        }
                        var restoreUser = CurrentUserId;
                        if (string.IsNullOrEmpty(restoreUser)) return RedirectToAction("Login", "Auth");
                        try
                        {
                            await _organizerService.RestoreEventAsync(restoreUser, id);
                            await ExecuteSuccessAsync("Khôi phục sự kiện thành công.", UserActionType.Restore, id, TargetType.Event);
                        }
                        catch (Exception ex)
                        {
                            await ExecuteErrorAsync(ex, "Đã xảy ra lỗi khi khôi phục sự kiện.");
                        }
                        return RedirectToAction("MyEventsDelete");

                    case "detailevent":
                    case "detail":
                        return await DetailEvent(id);

                    case "reanalyze-sentiment":
                        return await ReAnalyzeSentiment(id);

                    default:
                        return BadRequest("Operation không hợp lệ.");
                }
            }
            catch (Exception)
            {
                SetError("Đã xảy ra lỗi khi xử lý yêu cầu POST. Vui lòng thử lại.");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishEvent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetError("Event id không hợp lệ.");
                return RedirectToAction("MyEvents", "Organizer");
            }
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
            try
            {
                await _organizerService.PublishEventAsync(userId, id);
                await ExecuteSuccessAsync("Public event thành công.", UserActionType.Publish, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex is InvalidOperationException ? ex.Message : "Đã xảy ra lỗi khi public event. Vui lòng thử lại.");
            }
            return RedirectToAction("MyEvents", "Organizer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetError("Event id không hợp lệ.");
                return RedirectToAction("MyEvents", "Organizer");
            }
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
            try
            {
                await _organizerService.CancelEventAsync(userId, id);
                await ExecuteSuccessAsync("Hủy sự kiện thành công.", UserActionType.Cancel, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex is InvalidOperationException ? ex.Message : "Đã xảy ra lỗi khi hủy sự kiện. Vui lòng thử lại.");
            }
            return RedirectToAction("MyEvents", "Organizer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForApproval(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetError("Event id không hợp lệ.");
                return RedirectToAction("MyEvents", "Organizer");
            }
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
            try
            {
                await _organizerService.SendForApprovalAsync(userId, id);
                await ExecuteSuccessAsync("Gửi duyệt thành công.", UserActionType.Submit, id, TargetType.Event);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex is InvalidOperationException || ex is EventValidator.BusinessValidationException 
                    ? ex.Message 
                    : "Đã xảy ra lỗi khi gửi duyệt. Vui lòng thử lại.");
            }
            return RedirectToAction("MyEvents", "Organizer");
        }

        private async Task LoadDropdowns(CreateEventViewModel vm)
        {
            var dto = await _organizerService.GetCreateEventDropdownsAsync();
            _mapper.Map(dto, vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableLocations(DateTime startTime, DateTime endTime)
        {
            if (endTime <= startTime) return Json(new List<object>());
            var locations = await _locationService.GetAvailableLocationsAsync(startTime, endTime);
            return Json(locations.Select(x => new
            {
                id = x.LocationId,
                name = x.Name,
                address = x.Address,
                building = ExtractAddressPart(x.Address, "Building"),
                floor = ExtractAddressPart(x.Address, "Floor"),
                room = ExtractAddressPart(x.Address, "Room"),
                capacity = x.Capacity,
                type = x.Type?.ToString()
            }));
        }

        private static string ExtractAddressPart(string? address, string label)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;
            var segment = address.Split(" - ", StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.StartsWith(label + " ", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(segment)) return string.Empty;
            return segment.Substring(label.Length).Trim();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new CreateEventViewModel
            {
                Agendas = new List<CreateAgendaItemVm> { new CreateAgendaItemVm() }
            };
            ModelState.Remove("id");
            ModelState.Remove("operation");
            ModelState.Remove("legacyAction");
            await LoadDropdowns(vm);
            return View("~/Views/Event/CreateEvent.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel vm)
        {
            if (vm.EndTime <= vm.StartTime)
                ModelState.AddModelError(nameof(vm.EndTime), "EndTime phải lớn hơn StartTime");

            if (!vm.IsDepositRequired) vm.DepositAmount = 0;

            if (string.IsNullOrWhiteSpace(vm.LocationId))
            {
                ModelState.AddModelError(nameof(vm.LocationId), "Vui lòng chọn phòng hợp lệ.");
            }
            else
            {
                var selectedLocation = await _locationService.GetLocationByIdAsync(vm.LocationId);
                if (selectedLocation == null)
                {
                    ModelState.AddModelError(nameof(vm.LocationId), "Phòng đã chọn không tồn tại.");
                }
                else
                {
                    if (vm.MaxCapacity > selectedLocation.Capacity)
                    {
                        ModelState.AddModelError(nameof(vm.MaxCapacity), $"Sức chứa sự kiện phải nhỏ hơn hoặc bằng sức chứa phòng ({selectedLocation.Capacity}).");
                    }
                    if (vm.EndTime > vm.StartTime)
                    {
                        var availableLocations = await _locationService.GetAvailableLocationsAsync(vm.StartTime, vm.EndTime);
                        if (!availableLocations.Any(x => x.LocationId == vm.LocationId))
                        {
                            ModelState.AddModelError(nameof(vm.LocationId), "Phòng đã chọn không còn khả dụng trong khoảng thời gian này.");
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm);
                return View("~/Views/Event/CreateEvent.cshtml", vm);
            }

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            var dto = _mapper.Map<BusinessLogic.DTOs.Role.Organizer.CreateEventRequestDto>(vm);
            dto.RegistrationOpenTime = vm.StartTime.AddDays(-7);
            dto.RegistrationCloseTime = vm.StartTime.AddDays(-1);

            try
            {
                await _organizerService.CreateEventAsync(userId, dto);
                await ExecuteSuccessAsync("Tạo Event thành công (Draft).", UserActionType.Create, null, TargetType.Event);
                return RedirectToAction("Index", "Organizer");
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, ex.Message);
                await LoadDropdowns(vm);
                return View("~/Views/Event/CreateEvent.cshtml", vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            try
            {
                var dto = await _organizerService.GetEventDetailsAsync(id, CurrentUserId);

                await LogUserActivity(UserActionType.View, id, TargetType.Event, $"Đã xem chi tiết sự kiện '{dto.Title}'");

                var vm = _mapper.Map<AEMS_Solution.Models.Event.EventDetailsViewModel>(dto);
                vm.Teams = dto.Teams.Select(t => new AEMS_Solution.Models.Event.EventTeamVm
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    TeamName = t.TeamName,
                    Description = t.Description,
                    Score = t.Score,
                    PlaceRank = t.PlaceRank,
                    CreatedAt = t.CreatedAt,
                    TeamMembers = t.TeamMembers.Select(m => new AEMS_Solution.Models.Event.TeamMemberVm
                    {
                        Id = m.Id,
                        TeamId = m.TeamId,
                        StudentId = m.StudentId,
                        StaffId = m.StaffId,
                        MemberName = m.MemberName,
                        MemberEmail = m.MemberEmail,
                        RoleName = m.RoleName,
                        TeamRole = m.TeamRole
                    }).ToList()
                }).ToList();
                vm.ImageUrls = dto.ImageUrls;
                var dropdowns = await _organizerService.GetCreateEventDropdownsAsync();
                ViewBag.Locations = dropdowns.Locations;
                return View("~/Views/Event/DetailEvent.cshtml", vm);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyParticipatedEvents()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");
            var staffProfile = await _unitOfWork.StaffProfiles.GetAsync(x => x.UserId == CurrentUserId);
            if (staffProfile == null)
            {
                SetError("Không tìm thấy hồ sơ nhân sự.");
                return RedirectToAction(nameof(Index));
            }
            var events = await _unitOfWork.Events.GetAllAsync(
                e => e.DeletedAt == null && (
                     e.EventTeams.Any(et => et.DeletedAt == null && et.TeamMembers.Any(tm => tm.StaffId == staffProfile.Id || (tm.Staff != null && tm.Staff.UserId == CurrentUserId))) ||
                     e.EventAgenda.Any(a => (a.StaffSpeakerId == staffProfile.Id || (a.StaffSpeaker != null && a.StaffSpeaker.UserId == CurrentUserId)) && a.DeletedAt == null)),
                q => q.Include(x => x.Location)
                      .Include(x => x.Topic)
                      .Include(x => x.Semester)
                      .Include(x => x.EventTeams)
                        .ThenInclude(et => et.TeamMembers).ThenInclude(tm => tm.Staff)
                      .Include(x => x.EventAgenda).ThenInclude(a => a.StaffSpeaker));
            var vm = events
                .OrderBy(e => e.StartTime)
                .Select(e => new
                {
                    EventId = e.Id,
                    Title = e.Title,
                    Status = e.Status.ToString(),
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Location = e.Location?.Address ?? e.LocationId,
                    Role = e.EventTeams.Any(et => et.DeletedAt == null && et.TeamMembers.Any(tm => tm.StaffId == staffProfile.Id || (tm.Staff != null && tm.Staff.UserId == CurrentUserId))) ? "Ban tổ chức" : "Diễn giả"
                }).ToList();
            return View("~/Views/Organizer/MyParticipatedEvents.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEventTeamFromDetail(string EventId, string TeamName, string Description)
        {
            if (string.IsNullOrWhiteSpace(EventId) || string.IsNullOrWhiteSpace(TeamName)) return BadRequest();
            try
            {
                await _eventService.CreateEventTeamAsync(EventId, TeamName, Description);
                await ExecuteSuccessAsync("Đã tạo nhóm thành công.", UserActionType.Create, null, TargetType.None);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(DetailEvent), new { id = EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventTeamFromDetail(string TeamId, string EventId)
        {
            if (string.IsNullOrWhiteSpace(TeamId) || string.IsNullOrWhiteSpace(EventId)) return BadRequest();
            try
            {
                await _eventService.DeleteEventTeamAsync(TeamId);
                await ExecuteSuccessAsync("Đã xóa nhóm.", UserActionType.Delete, TeamId, TargetType.None);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(DetailEvent), new { id = EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMemberToTeam(string TeamId, string EventId, string? StudentId, string? StaffId, string RoleName)
        {
            if (string.IsNullOrWhiteSpace(TeamId) || string.IsNullOrWhiteSpace(EventId)) return BadRequest();
            try
            {
                await _eventService.AddMemberToTeamAsync(TeamId, StudentId, StaffId, RoleName);
                await ExecuteSuccessAsync("Đã thêm thành viên vào nhóm.", UserActionType.AddMember, TeamId, TargetType.None);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(DetailEvent), new { id = EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMemberFromTeam(string MemberId, string EventId)
        {
            if (string.IsNullOrWhiteSpace(MemberId) || string.IsNullOrWhiteSpace(EventId)) return BadRequest();
            try
            {
                await _eventService.RemoveMemberFromTeamAsync(MemberId);
                await ExecuteSuccessAsync("Đã xóa thành viên khỏi nhóm.", UserActionType.RemoveMember, MemberId, TargetType.None);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(DetailEvent), new { id = EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAgendaFromDetail(CreateDetailAgendaViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EventId))
            {
                SetError("Event id không hợp lệ.");
                return RedirectToAction(nameof(MyEvents));
            }
            if (string.IsNullOrWhiteSpace(CurrentUserId)) return RedirectToAction("Login", "Auth");
            var currentStaff = await _unitOfWork.StaffProfiles.GetAsync(x => x.UserId == CurrentUserId);
            if (currentStaff == null)
            {
                SetError("Chưa thiết lập hồ sơ nhân viên (StaffProfile).");
                return RedirectToAction(nameof(MyEvents));
            }
            if (!ModelState.IsValid)
            {
                await ExecuteErrorAsync(new Exception("Dữ liệu agenda không hợp lệ."), ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage);
                return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
            }
            var dto = new BusinessLogic.DTOs.Role.Organizer.CreateEventAgendaDto
            {
                EventId = model.EventId,
                SessionName = model.SessionName,
                SpeakerInfo = model.SpeakerInfo,
                SpeakerUserId = model.SpeakerUserId,
                SpeakerUserRole = model.SpeakerUserRole,
                Description = model.Description,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Location = model.Location
            };
            try
            {
                await _eventService.CreateEventAgendaAsync(CurrentUserId, dto);
                await ExecuteSuccessAsync("Tạo agenda thành công.", UserActionType.Create, model.EventId, TargetType.Event);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDocumentFromDetail(CreateDetailDocumentViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EventId))
            {
                SetError("Event id không hợp lệ.");
                return RedirectToAction(nameof(MyEvents));
            }
            if (string.IsNullOrWhiteSpace(CurrentUserId)) return RedirectToAction("Login", "Auth");
            var staff = await _unitOfWork.StaffProfiles.GetAsync(x => x.UserId == CurrentUserId);
            if (staff == null)
            {
                SetError("Chưa thiết lập hồ sơ nhân viên (StaffProfile).");
                return RedirectToAction(nameof(MyEvents));
            }
            if (string.IsNullOrWhiteSpace(model.Url) && (model.Files == null || !model.Files.Any()))
            {
                ModelState.AddModelError("", "Vui lòng cung cấp Link tài liệu hoặc Tải file lên.");
            }
            if (!ModelState.IsValid)
            {
                SetError(ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Dữ liệu tài liệu không hợp lệ.");
                return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
            }
            int successCount = 0;
            try
            {
                if (model.Files != null && model.Files.Any())
                {
                    foreach (var file in model.Files)
                    {
                        var uploadResult = await _fileStorageService.UploadSingleAsync(file, DataAccess.Enum.UploadContext.Material, CurrentUserId);
                        if (uploadResult != null)
                        {
                            var dto = new BusinessLogic.DTOs.Role.Organizer.CreateEventDocumentDto
                            {
                                EventId = model.EventId,
                                Name = string.IsNullOrWhiteSpace(model.Name) ? file.FileName : (model.Files.Count > 1 ? $"{model.Name} - {file.FileName}" : model.Name),
                                Url = uploadResult.Url,
                                Type = model.Type
                            };
                            await _eventService.CreateEventDocumentAsync(CurrentUserId, dto);
                            successCount++;
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(model.Url))
                {
                    var dto = new BusinessLogic.DTOs.Role.Organizer.CreateEventDocumentDto
                    {
                        EventId = model.EventId,
                        Name = string.IsNullOrWhiteSpace(model.Name) ? "External Document" : model.Name,
                        Url = model.Url.Trim(),
                        Type = model.Type
                    };
                    await _eventService.CreateEventDocumentAsync(CurrentUserId, dto);
                    successCount++;
                }
                if (successCount > 0)
                {
                    await ExecuteSuccessAsync($"Tạo thành công {successCount} document(s).", UserActionType.Create, model.EventId, TargetType.Event);
                }
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, "Có lỗi xảy ra: " + ex.Message); }
            return RedirectToAction(nameof(Manage), new { operation = "detailevent", id = model.EventId });
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents(string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
            try
            {
                var paged = await _organizerService.GetMyEventsAsync(userId, search, status, semesterId, page, pageSize);
                var vm = new MyEventsViewModel
                {
                    Events = _mapper.Map<List<OrganizerEventCardVm>>(paged.Items),
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                    TotalItems = paged.Total,
                    Search = search,
                    Status = status,
                    SemesterId = semesterId
                };
                return View("~/Views/Event/MyEvent.cshtml", vm);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, $"Lỗi: {ex.Message}");
                return View("~/Views/Event/MyEvent.cshtml", new MyEventsViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyEventsDelete(string? search, EventStatusEnum? status, string? semesterId, int page = 1, int pageSize = 10)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
            try
            {
                var paged = await _organizerService.GetMyDeletedEventsAsync(userId, search, status, semesterId, page, pageSize);
                var vm = new MyEventsViewModel
                {
                    Events = _mapper.Map<List<OrganizerEventCardVm>>(paged.Items),
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                    TotalItems = paged.Total,
                    Search = search,
                    Status = status,
                    SemesterId = semesterId
                };
                return View("~/Views/Event/MyEventDelete.cshtml", vm);
            }
            catch (Exception ex)
            {
                SetError($"Lỗi: {ex.Message}");
                return View("~/Views/Event/MyEventDelete.cshtml", new MyEventsViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");
                var dto = await _organizerService.GetDashboardAsync(userId);
                var vm = _mapper.Map<OrganizerDashboardViewModel>(dto);
                return View(vm);
            }
            catch (Exception ex)
            {
                await ExecuteErrorAsync(ex, $"Lỗi: {ex.Message}");
                return View(new OrganizerDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Participants(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            try
            {
                var participants = await _organizerService.GetParticipantsAsync(id);
                var eventDetail = await _organizerService.GetEventDetailsAsync(id, CurrentUserId);
                var vm = new EventParticipantsViewModel
                {
                    EventId = id,
                    EventTitle = eventDetail.Title,
                    Participants = participants
                };
                return View("~/Views/Organizer/Participants.cshtml", vm);
            }
            catch (Exception ex)
            {
                SetError("Đã xảy ra lỗi khi tải danh sách người tham gia: " + ex.Message);
                return RedirectToAction(nameof(Manage), new { operation = "detail", id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualRegister(string eventId, string userId)
        {
            if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(userId)) return BadRequest();
            try
            {
                await _organizerService.ManualRegisterAsync(eventId, userId, CurrentUserId);
                await ExecuteSuccessAsync("Đăng ký thành công.", UserActionType.Register, eventId, TargetType.Event);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(Manage), new { operation = "participants", id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(string ticketId, string eventId)
        {
            if (string.IsNullOrEmpty(ticketId) || string.IsNullOrEmpty(eventId)) return BadRequest();
            try
            {
                await _organizerService.CancelTicketAsync(ticketId, CurrentUserId);
                await ExecuteSuccessAsync("Đã hủy vé thành công.", UserActionType.Cancel, ticketId, TargetType.None);
            }
            catch (Exception ex) { await ExecuteErrorAsync(ex, ex.Message); }
            return RedirectToAction(nameof(Manage), new { operation = "participants", id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendTicket(string ticketId, string eventId)
        {
            if (string.IsNullOrEmpty(ticketId) || string.IsNullOrEmpty(eventId)) return BadRequest();
            try
            {
                await _organizerService.ResendTicketEmailAsync(ticketId, CurrentUserId);
                SetSuccess("Đã gửi lại email vé thành công.");
            }
            catch (Exception ex) { SetError(ex.Message); }
            return RedirectToAction(nameof(Manage), new { operation = "participants", id = eventId });
        }
        [HttpGet]
        public async Task<IActionResult> ViewFeedback(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                SetError("Event ID không hợp lệ.");
                return RedirectToAction("Index");
            }
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Auth");

            try
            {
                var dto = new BusinessLogic.DTOs.Event.EventFeedbackSummary.FeedbackForOrganizerDTO { EventId = id };
                var feedbacks = await _eventService.ShowFeedbackForOrganizer(userId, dto);
                ViewBag.EventTitle = feedbacks.FirstOrDefault()?.EventTitle ?? "Event";
                ViewBag.EventId = id;
                ViewBag.AvgRating = feedbacks.Count > 0 ? feedbacks.First().AvgRating : 0;
                ViewBag.TotalFeedbacks = feedbacks.Count;
                return View("~/Views/Organizer/ViewFeedBack.cshtml", feedbacks);
            }
            catch (InvalidOperationException ex)
            {
                SetError(ex.Message);
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                SetError("Đã xảy ra lỗi khi tải feedback.");
                return RedirectToAction("Index");
            }
        }
    }
}
