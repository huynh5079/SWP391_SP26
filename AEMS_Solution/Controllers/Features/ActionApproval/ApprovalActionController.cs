using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Event;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Approval;
using BusinessLogic.Service.Event;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Enum;
namespace AEMS_Solution.Controllers.Features.ActionApproval;

[Authorize(Roles = "Approver")]
public class ApprovalActionController : BaseController
{
    private readonly IApproverCommandService _approverCommandService;

    public ApprovalActionController(IApproverCommandService approverCommandService)
    {
        _approverCommandService = approverCommandService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveAsync(string? operation, string? eventId, string? comment)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            SetError("Event id không hợp lệ.");
            return RedirectToAction("Index", "Approver");
        }

        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
        {
            SetError("Người dùng chưa đăng nhập.");
            return RedirectToAction("Login", "Auth");
        }

        try
        {
            var op = (operation ?? string.Empty).Trim().ToLowerInvariant();
            switch (op)
            {
                case "approve":
                case "approved":
                    await _approverCommandService.ApproveAsync(eventId, userId, comment);
                    await ExecuteSuccessAsync("Duyệt thành công.", UserActionType.Approve, eventId, TargetType.Event);
                    break;
                case "reject":
                case "rejected":
                    await _approverCommandService.RejectAsync(eventId, userId, comment);
                    await ExecuteSuccessAsync("Từ chối thành công.", UserActionType.Reject, eventId, TargetType.Event);
                    break;
                case "requestchange":
                case "request-change":
                case "request_change":
                case "request":
                    await _approverCommandService.RequestChangeAsync(eventId, userId, comment);
                    await ExecuteSuccessAsync("Yêu cầu chỉnh sửa đã gửi.", UserActionType.Update, eventId, TargetType.Event);
                    break;
                default:
                    SetError("Hành động không hợp lệ.");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            await ExecuteErrorAsync(ex, ex.Message);
        }

        return RedirectToAction("Index", "Approver");
    }
}
