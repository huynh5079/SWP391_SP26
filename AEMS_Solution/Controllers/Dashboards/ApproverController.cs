using System.Threading.Tasks;
using AEMS_Solution.BaseAction_ValidforController_.Approver.Agenda;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event.EventAgenda;
using AutoMapper;
using AEMS_Solution.Models.Approver;
using BusinessLogic.Service.Approval;
using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.Service.Event.Sub_Service.Location;
using DataAccess.Repositories.Abstraction;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AEMS_Solution.Models.Approver.Manage;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Approver")]
    public class ApproverController : BaseController
    {
        private readonly IApproverQueryService _queryService;
        private readonly IApproverCommandService _commandService;
        private readonly ILocationService _locationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IApproverEventAgendaAction _eventAgendaAction;

        public ApproverController(IApproverQueryService queryService, IApproverCommandService commandService, ILocationService locationService, IUnitOfWork unitOfWork, IMapper mapper, IApproverEventAgendaAction eventAgendaAction)
        {
            _queryService = queryService;
            _commandService = commandService;
            _locationService = locationService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _eventAgendaAction = eventAgendaAction;
        }

        [HttpGet]
        public async Task<IActionResult> Agenda(string? search = null, string? eventId = null)
        {
            try
            {
                _eventAgendaAction.EnsureApproverId(CurrentUserId);

                var events = (await _unitOfWork.Events.GetAllAsync(x => x.DeletedAt == null))
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
                        || (x.SpeakerName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
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
        public async Task<IActionResult> PendingApprovals(string? search, int page = 1, int pageSize = 20)
        {
            var list = await _queryService.GetPendingEventsAsync(search, null, page, pageSize);

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
					LastApprovalComment = e.LastApprovalComment
				});
            }

            vm.Search = search;
            vm.Page = page;
            vm.PageSize = pageSize;

            return View("~/Views/Approval/PendingApprovals.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var allEvents = await _unitOfWork.Events.GetAllAsync(includes: q => q.Include(e => e.Location));
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

            foreach(var e in recentPending)
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
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                MaxCapacity = dto.MaxCapacity,
                Status = dto.Status,
                OrganizerName = dto.OrganizerName,
                OrganizerEmail = dto.OrganizerEmail
            };

            foreach (var l in dto.ApprovalLogs)
            {
                vm.ApprovalLogs.Add(new ApprovalLogVm
                {
                    ApproverId = l.ApproverId,
                    Action = l.Action,
                    Comment = l.Comment,
                    CreatedAt = l.CreatedAt
                });
            }

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
