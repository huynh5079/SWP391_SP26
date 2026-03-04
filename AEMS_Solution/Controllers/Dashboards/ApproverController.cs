using System.Threading.Tasks;
using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Approver;
using BusinessLogic.Service.Approval;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AEMS_Solution.Controllers.Dashboards
{
    [Authorize(Roles = "Approver")]
    public class ApproverController : BaseController
    {
        private readonly IApproverQueryService _queryService;
        private readonly IApproverCommandService _commandService;

        public ApproverController(IApproverQueryService queryService, IApproverCommandService commandService)
        {
            _queryService = queryService;
            _commandService = commandService;
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
