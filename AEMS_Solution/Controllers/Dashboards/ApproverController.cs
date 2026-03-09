using System.Threading.Tasks;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Approver;
using BusinessLogic.Service.Approval;
using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.Service.Event.Sub_Service.Location;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AEMS_Solution.Models.Approver.Manage;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Approver")]
    public class ApproverController : BaseController
    {
        private readonly IApproverQueryService _queryService;
        private readonly IApproverCommandService _commandService;
        private readonly ILocationService _locationService;

        public ApproverController(IApproverQueryService queryService, IApproverCommandService commandService, ILocationService locationService)
        {
            _queryService = queryService;
            _commandService = commandService;
            _locationService = locationService;
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
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            var list = await _queryService.GetPendingEventsAsync(search, null, page, pageSize);

            var vm = new ApproverDashboardViewModel();
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
