using AEMS_Solution.BaseAction_ValidforController_.Approver.Agenda;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Approver;
using AEMS_Solution.Models.Approver.Manage;
using AEMS_Solution.Models.Event.EventAgenda;
using AEMS_Solution.Models.Event.Semester;
using AutoMapper;
using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.DTOs.Event.Semester;
using BusinessLogic.Service.Approval;
using BusinessLogic.Service.Event.Sub_Service.Location;
using BusinessLogic.Service.Event.Sub_Service.Semester;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Approver")]
    public class ApproverController : BaseController
    {
        private readonly IApproverQueryService _queryService;
        private readonly IApproverCommandService _commandService;
        private readonly ILocationService _locationService;
        private readonly ISemesterService _semesterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IApproverEventAgendaAction _eventAgendaAction;
        private readonly BusinessLogic.Service.Event.IEventService _eventService;

        public ApproverController(IApproverQueryService queryService, IApproverCommandService commandService, ILocationService locationService, ISemesterService semesterService, IUnitOfWork unitOfWork, IMapper mapper, IApproverEventAgendaAction eventAgendaAction, BusinessLogic.Service.Event.IEventService eventService)
        {
            _queryService = queryService;
            _commandService = commandService;
            _locationService = locationService;
            _semesterService = semesterService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _eventAgendaAction = eventAgendaAction;
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> Semester(string? search = null, string? status = null)
        {
            var semesters = await _semesterService.GetAllSemestersAsync();
            var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
            var canCreateNextSemester = !semesters.Any(x => x.StartDate.HasValue && x.StartDate.Value > now);

            var filtered = semesters.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                filtered = filtered.Where(x =>
                    (x.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (x.Code?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SemesterStatusEnum>(status, true, out var parsedStatus))
            {
                filtered = filtered.Where(x => x.Status == parsedStatus);
            }

            var model = new ApproverSemesterViewModel
            {
                Search = search,
                Status = status,
                CanCreateNextSemester = canCreateNextSemester,
                Semesters = _mapper.Map<List<SemesterItemViewModel>>(filtered.OrderByDescending(x => x.StartDate).ToList())
            };

            return View("~/Views/Event/Semester/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSemester()
        {
            var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
            var model = new SemesterDTO
            {
                Name = "Spring",
                year = now.Year,
                Code = $"SP{(now.Year % 100):00}",
                StartDate = new DateTime(now.Year, 1, 1),
                EndDate = new DateTime(now.Year, 4, 30),
                Status = SemesterStatusEnum.Upcoming
            };

            var allSemesters = await _semesterService.GetAllSemestersAsync();
            var canCreate = !allSemesters.Any(x => x.StartDate.HasValue && x.StartDate.Value > now);
            if (!canCreate)
            {
                SetInfo("Đã có semester kế tiếp. Nút tạo mới sẽ mở lại khi semester đó bắt đầu.");
                return RedirectToAction(nameof(Semester));
            }

            return View("~/Views/Event/Semester/CreateSemester.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditSemester(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                SetError("SemesterId không hợp lệ.");
                return RedirectToAction(nameof(Semester));
            }

            var semester = await _semesterService.GetSemesterByIdAsync(id);
            if (semester == null)
            {
                SetError("Không tìm thấy học kỳ.");
                return RedirectToAction(nameof(Semester));
            }

            return View("~/Views/Event/Semester/CreateSemester.cshtml", semester);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoCreateSemester()
        {
            try
            {
                var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
                var allSemesters = await _semesterService.GetAllSemestersAsync();
                if (allSemesters.Any(x => x.StartDate.HasValue && x.StartDate.Value > now))
                {
                    SetInfo("Đã có semester kế tiếp. Khi semester đó bắt đầu, bạn mới tạo tiếp được.");
                    return RedirectToAction(nameof(Semester));
                }

                var created = await _semesterService.AutoCreateSemesterAsync();
                SetSuccess($"Đã tự động tạo học kỳ {created.Name} ({created.Code}).");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(Semester));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemester(SemesterDTO dto)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.SemesterId))
                {
                    await _semesterService.UpdateSemesterAsync(dto.SemesterId, dto);
                    SetSuccess("Cập nhật học kỳ thành công.");
                    return RedirectToAction(nameof(Semester));
                }

                var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
                var allSemesters = await _semesterService.GetAllSemestersAsync();
                if (allSemesters.Any(x => x.StartDate.HasValue && x.StartDate.Value > now))
                {
                    SetInfo("Đã có semester kế tiếp. Khi semester đó bắt đầu, bạn mới tạo tiếp được.");
                    return RedirectToAction(nameof(Semester));
                }

                await _semesterService.CreateSemesterAsync(dto);
                SetSuccess("Tạo học kỳ thủ công thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(Semester));
        }

        [HttpGet]
        public async Task<IActionResult> Agenda(string? search = null, string? eventId = null)
        {
            try
            {
                _eventAgendaAction.EnsureApproverId(CurrentUserId);

                var events = (await _unitOfWork.Events.GetAllAsync(x => x.DeletedAt == null && x.Status != EventStatusEnum.Expired))
                    .OrderBy(x => x.Title)
                    .ToList();

                var agendas = (await _unitOfWork.EventAgenda.GetAllAsync(
                    x => x.DeletedAt == null,
                    q => q
                        .Include(x => x.Event)
                            .ThenInclude(x => x.Organizer)
                                .ThenInclude(x => x.User)))
                    .Where(x => x.Event != null && x.Event.DeletedAt == null);

                if (!string.IsNullOrWhiteSpace(eventId))
                {
                    agendas = agendas.Where(x => x.EventId == eventId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var keyword = search.Trim();
                    agendas = agendas.Where(x =>
                        (x.SessionName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.SpeakerInfo?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.Event?.Title?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.Event?.Organizer?.User?.FullName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                var model = new MyAgendaViewModel
                {
                    PageTitle = "All Agenda",
                    PageDescription = "Approver có thể xem toàn bộ agenda của các organizer.",
                    IsReadOnly = true,
                    Search = search,
                    SelectedEventId = eventId,
                    EventOptions = events.Select(x => new SelectListItem
                    {
                        Value = x.Id,
                        Text = x.Title,
                        Selected = x.Id == eventId
                    }).ToList(),
                    Agendas = _mapper.Map<List<AgendaItemViewModel>>(agendas.OrderBy(x => x.StartTime ?? DateTime.MaxValue).ThenBy(x => x.SessionName).ToList())
                };

                return View("~/Views/Event/Agenda/MyAgenda.cshtml", model);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Auth");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyParticipatedEvents()
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            var staffProfile = await _unitOfWork.StaffProfiles.GetAsync(x => x.UserId == CurrentUserId);
            if (staffProfile == null)
            {
                return View("~/Views/Organizer/MyParticipatedEvents.cshtml", new List<object>());
            }

            var events = await _unitOfWork.Events.GetAllAsync(
                e => e.DeletedAt == null && (
                     e.EventTeams.Any(et => et.TeamMembers.Any(tm => tm.StaffId == staffProfile.Id)) ||
                     e.EventAgenda.Any(a => a.StaffSpeakerId == staffProfile.Id && a.DeletedAt == null)),
                q => q.Include(x => x.Location)
                      .Include(x => x.Topic)
                      .Include(x => x.Semester)
                      .Include(x => x.EventTeams)
                        .ThenInclude(et => et.TeamMembers)
                      .Include(x => x.EventAgenda));

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
                    Role = e.EventTeams.Any(et => et.TeamMembers.Any(tm => tm.StaffId == staffProfile.Id))
                        ? "Ban tổ chức"
                        : "Diễn giả"
                })
                .ToList();

            return View("~/Views/Organizer/MyParticipatedEvents.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoom()
        {
            var locations = await _locationService.GetAllLocationsAsync();
            var vm = new ManageRoomViewModel
            {
                Rooms = locations.Select(x => new RoomListItemVm
                {
                    LocationId = x.LocationId,
                    Name = x.Name,
                    Address = x.Address,
                    Capacity = x.Capacity,
                    Status = x.Status,
                    Type = x.Type,
                    Description = x.Description
                }).ToList(),
                NewRoom = new CreateRoomViewModel()
            };

            return View("~/Views/Approval/ManageLocation.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(ManageRoomViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Approval/ManageLocation.cshtml", await BuildManageRoomViewModelAsync(vm.NewRoom));
            }

            try
            {
                vm.NewRoom.Address = BuildAddress(vm.NewRoom);

                await _locationService.CreateLocationAsync(new CreateLocationDTO
                {
                    Name = vm.NewRoom.Name,
                    Address = vm.NewRoom.Address,
                    Capacity = vm.NewRoom.Capacity,
                    Status = vm.NewRoom.Status,
                    Type = vm.NewRoom.Type,
                    Description = vm.NewRoom.Description
                });
                SetSuccess("Tạo phòng thành công.");
                return RedirectToAction(nameof(ManageRoom));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Approval/ManageLocation.cshtml", await BuildManageRoomViewModelAsync(vm.NewRoom));
            }
        }

        private static string BuildAddress(CreateRoomViewModel room)
        {
            var parts = new[]
            {
                FormatAddressPart(room.Building, "Building"),
                FormatAddressPart(room.Floor, "Floor"),
                FormatAddressPart(room.Room, "Room")
            }
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return string.Join(" - ", parts!);
        }

        private static string? FormatAddressPart(string? value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            return $"{prefix} {normalized}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoomStatus(UpdateRoomStatusViewModel vm)
        {
            var location = await _locationService.GetLocationByIdAsync(vm.LocationId);
            if (location == null)
            {
                SetError("Không tìm thấy phòng.");
                return RedirectToAction(nameof(ManageRoom));
            }

            try
            {
                await _locationService.UpdateLocationAsync(vm.LocationId, new UpdateLocationDTO
                {
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity,
                    Status = vm.Status,
                    Type = location.Type,
                    Description = location.Description
                });
                SetSuccess("Cập nhật trạng thái phòng thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(ManageRoom));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoom(UpdateRoomViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.");
                return RedirectToAction(nameof(ManageRoom));
            }

            var location = await _locationService.GetLocationByIdAsync(vm.LocationId);
            if (location == null)
            {
                SetError("Không tìm thấy phòng.");
                return RedirectToAction(nameof(ManageRoom));
            }

            try
            {
                // Build address from parts (same logic as CreateRoom)
                var addressParts = new[] {
                    FormatAddressPart(vm.Building, "Building"),
                    FormatAddressPart(vm.Floor, "Floor"),
                    FormatAddressPart(vm.Room, "Room")
                }.Where(x => !string.IsNullOrWhiteSpace(x));
                var builtAddress = string.Join(" - ", addressParts!);
                var finalAddress = string.IsNullOrWhiteSpace(builtAddress) ? vm.Building ?? location.Address : builtAddress;

                await _locationService.UpdateLocationAsync(vm.LocationId, new UpdateLocationDTO
                {
                    Name = vm.Name.Trim(),
                    Address = finalAddress,
                    Capacity = vm.Capacity,
                    Status = vm.Status,
                    Type = vm.Type,
                    Description = vm.Description ?? string.Empty
                });
                SetSuccess("Cập nhật phòng thành công.");
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }

            return RedirectToAction(nameof(ManageRoom));
        }

        private async Task<ManageRoomViewModel> BuildManageRoomViewModelAsync(CreateRoomViewModel? createRoom = null)
        {
            var locations = await _locationService.GetAllLocationsAsync();
            return new ManageRoomViewModel
            {
                Rooms = locations.Select(x => new RoomListItemVm
                {
                    LocationId = x.LocationId,
                    Name = x.Name,
                    Address = x.Address,
                    Capacity = x.Capacity,
                    Status = x.Status,
                    Type = x.Type,
                    Description = x.Description
                }).ToList(),
                NewRoom = createRoom ?? new CreateRoomViewModel()
            };
        }

        [HttpGet]
        public async Task<IActionResult> AllEvents(string? search, string? status = "", int page = 1, int pageSize = 20)
        {
            var statusFilter = string.IsNullOrWhiteSpace(status) || status.Trim().Equals("all", System.StringComparison.OrdinalIgnoreCase)
                ? null
                : status;

            var list = await _queryService.GetPendingEventsAsync(CurrentUserId, search, statusFilter, page, pageSize);

            var vm = new PendingApprovalsViewModel();
            foreach (var e in list)
            {
                vm.Events.Add(new ApproverEventCardVm
                {
                    EventId = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime ?? System.DateTime.MinValue,
                    Status = e.Status,
                    ThumbnailUrl = e.ThumbnailUrl,
                    Location = e.Location,
                    OrganizerName = e.OrganizerName,
                    OrganizerEmail = e.OrganizerEmail,
                    LastApprovalComment = e.LastApprovalComment
                });
            }

            vm.Search = search;
            vm.Status = status ?? string.Empty;
            vm.Page = page;
            vm.PageSize = pageSize;

            return View("~/Views/Approval/AllEvents.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ViewFeedBack(string id)
        {
            try
            {
                // We use the new ShowFeedbackForApprover bypass method
                var feedbacks = await _eventService.ShowFeedbackForApprover(id);
                return View("~/Views/Approval/ViewFeedBack.cshtml", feedbacks);
            }
            catch (System.Exception ex)
            {
                TempData["NotificationMessage"] = ex.Message;
                TempData["NotificationType"] = "error";
                return RedirectToAction(nameof(AllEvents));
            }
        }

        [HttpGet]
        public async Task<IActionResult> PendingApprovals(string? search, string? status = "Pending", int page = 1, int pageSize = 20)
        {
            // Treat empty string or "all" as requesting all statuses (null passed to query service)
            var statusFilter = string.IsNullOrWhiteSpace(status) || status.Trim().Equals("all", System.StringComparison.OrdinalIgnoreCase)
                ? null
                : status;

            // Pass computed statusFilter to the query service so tabs (Approved/Rejected/All) actually filter results
            var list = await _queryService.GetPendingEventsAsync(CurrentUserId, search, statusFilter, page, pageSize);

            var vm = new PendingApprovalsViewModel();
            foreach (var e in list)
            {
                vm.Events.Add(new ApproverEventCardVm
                {
                    EventId = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime ?? System.DateTime.MinValue,
                    Status = e.Status,
                    ThumbnailUrl = e.ThumbnailUrl,
                    Location = e.Location,
                    OrganizerName = e.OrganizerName,
                    OrganizerEmail = e.OrganizerEmail,
                    LastApprovalComment = e.LastApprovalComment
                });
            }

            vm.Search = search;
            // Keep the original status value for tab highlighting in the view (empty string for All)
            vm.Status = status ?? string.Empty;
            vm.Page = page;
            vm.PageSize = pageSize;

            return View("~/Views/Approval/PendingApprovals.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var approverStaffId = (await _unitOfWork.StaffProfiles.GetAsync(x => x.UserId == CurrentUserId))?.Id;
            var allEvents = await _unitOfWork.Events.GetAllAsync(
                e => e.DeletedAt == null && (approverStaffId == null || e.OrganizerId != approverStaffId),
                includes: q => q.Include(e => e.Location)
                    .Include(e => e.Organizer!)
                        .ThenInclude(o => o!.User));
            var pendingEvents = allEvents.Where(x => x.Status == EventStatusEnum.Pending).ToList();

            var vm = new ApproverDashboardStatsViewModel
            {
                TotalEventsPending = pendingEvents.Count,
                TotalEventsApproved = allEvents.Count(x => x.Status == EventStatusEnum.Approved),
                TotalEventsRejected = allEvents.Count(x => x.Status == EventStatusEnum.Rejected),
                EventsAwaitingAction = pendingEvents.Count
            };

            var recentPending = pendingEvents
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var e in recentPending)
            {
                vm.RecentPendingEvents.Add(new ApproverEventCardVm
                {
                    EventId = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Status = e.Status,
                    ThumbnailUrl = e.ThumbnailUrl,
                    Location = e.Location?.Address,
                    OrganizerName = e.Organizer?.User?.FullName,
                    OrganizerEmail = e.Organizer?.User?.Email,
                    LastApprovalComment = null
                });
            }

            return View("~/Views/Approval/Index.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var dto = await _queryService.GetEventDetailAsync(id);
            if (dto == null) return NotFound();

            var vm = new ApproverEventDetailVm
            {
                EventId = dto.EventId,
                ThumbnailUrl = dto.ThumbnailUrl,
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                MaxCapacity = dto.MaxCapacity,
                Status = dto.Status,
                OrganizerName = dto.OrganizerName,
                OrganizerEmail = dto.OrganizerEmail,
                Location = dto.Location,

                // Agendas
                Agendas = dto.Agendas.Select(a => new AgendaVm
                {
                    Title = a.Title,
                    Description = a.Description,
                    Speaker = a.Speaker,
                    StartTime = a.StartTime ?? dto.StartTime,
                    EndTime = a.EndTime ?? dto.EndTime,
                    Location = a.Location
                }).ToList(),

                // Documents
                Documents = dto.Documents.Select(d => new DocumentVm
                {
                    FileName = d.FileName,
                    FileUrl = d.FileUrl,
                    FileSizeBytes = 0,// EventDocument entity không có SizeBytes
                    Type = d.Type 
                }).ToList(),

                // Approval Logs
                ApprovalLogs = dto.ApprovalLogs.Select(l => new ApprovalLogVm
                {
                    ApproverId = l.ApproverId,
                    Action = l.Action,
                    Comment = l.Comment,
                    CreatedAt = l.CreatedAt,
                }).ToList(),
            };

            return View("~/Views/Approval/Detail.cshtml", vm);
        }

        [HttpGet]
        public IActionResult DescriptAction(string id, string operation)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(operation))
            {
                SetError("Event hoặc hành động không hợp lệ.");
                return RedirectToAction("Index");
            }

            var op = operation.Trim().ToLowerInvariant();
            var actionName = op switch
            {
                "approve" or "approved" => "Approve",
                "reject" or "rejected" => "Reject",
                "requestchange" or "request-change" or "request_change" or "request" => "RequestChange",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(actionName))
            {
                SetError("Hành động không hợp lệ.");
                return RedirectToAction("Index");
            }

            var vm = new Models.Approver.ApproverActionFormVm
            {
                EventId = id,
                Operation = actionName,
                Heading = actionName switch
                {
                    "Approve" => "Phê duyệt sự kiện",
                    "Reject" => "Từ chối sự kiện",
                    _ => "Yêu cầu chỉnh sửa"
                }
            };

            return View("~/Views/Approval/DescriptAction.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string? comment)
        {
            try
            {
                var userId = CurrentUserId;
                await _commandService.ApproveAsync(id, userId ?? string.Empty, comment);
                SetSuccess("Duyệt thành công.");
            }
            catch (System.Exception ex)
            {
                SetError(ex.Message);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string? comment)
        {
            try
            {
                var userId = CurrentUserId;
                await _commandService.RejectAsync(id, userId ?? string.Empty, comment);
                SetSuccess("Từ chối thành công.");
            }
            catch (System.Exception ex)
            {
                SetError(ex.Message);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChange(string id, string? comment)
        {
            try
            {
                var userId = CurrentUserId;
                await _commandService.RequestChangeAsync(id, userId ?? string.Empty, comment);
                SetSuccess("Yêu cầu chỉnh sửa đã gửi.");
            }
            catch (System.Exception ex)
            {
                SetError(ex.Message);
            }
            return RedirectToAction("Index");
        }
    }
}
