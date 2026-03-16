using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Organizer.BudgetProposal;
using BusinessLogic.Service.Organizer.BudgetProposal;
using BusinessLogic.Service.System;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AEMS_Solution.Controllers.Features.ActionApproval
{
    [Authorize(Roles = "Approver")]
    public class BudgetApprovalController : BaseController
    {
        private readonly IBudgetProposalService _service;
        private readonly ISystemErrorLogService _errorLog;

        public BudgetApprovalController(
            IBudgetProposalService service,
            ISystemErrorLogService errorLog)
        {
            _service = service;
            _errorLog = errorLog;
        }

        // ─── Helper: build ViewModel ─────────────────────────────────────────
        private async Task<BudgetProposalViewModel> BuildViewModelAsync(string eventId)
        {
            var vm = new BudgetProposalViewModel
            {
                EventId = eventId,
                IsApprover = true
            };

            try
            {
                var proposal = await _service.GetByEventAsync(eventId);
                var receipts = await _service.GetReceiptsByProposalAsync(proposal.ProposalId);
                var summary = await _service.GetSummaryAsync(proposal.ProposalId);

                vm.ProposalId = proposal.ProposalId;
                vm.Title = proposal.Title;
                vm.Description = proposal.Description;
                vm.PlannedAmount = proposal.PlannedAmount;
                vm.ActualAmount = proposal.ActualAmount;
                vm.Variance = proposal.Variance;
                vm.Note = proposal.Note;
                vm.ApprovedBy = proposal.ApprovedBy;
                vm.ApprovedAt = proposal.ApprovedAt;

                if (Enum.TryParse<ProposalStatusEnum>(proposal.Status, out var status))
                    vm.Status = status;

                vm.Items = proposal.Items.Select(i => new BudgetItemViewModel
                {
                    ItemId = i.ItemId,
                    Category = i.Category,
                    Description = i.Description,
                    EstimatedAmount = i.EstimatedAmount
                }).ToList();

                vm.Receipts = receipts.Select(r =>
                {
                    Enum.TryParse<ExpenseStatusEnum>(r.Status, out var es);
                    return new ExpenseReceiptViewModel
                    {
                        ReceiptId = r.ReceiptId,
                        Title = r.Title,
                        ActualAmount = r.ActualAmount,
                        ReceiptImageUrl = r.ReceiptImageUrl,
                        SubmittedBy = r.SubmittedBy,
                        Status = es,
                        CreatedAt = r.CreatedAt
                    };
                }).ToList();

                vm.ItemSummaries = summary.ItemSummaries.Select(s => new BudgetItemSummaryViewModel
                {
                    Category = s.Category,
                    EstimatedAmount = s.EstimatedAmount,
                    ActualAmount = s.ActualAmount,
                    Variance = s.Variance
                }).ToList();
            }
            catch
            {
                // Chưa có Proposal
            }

            return vm;
        }

        // ─── Review ──────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Review(string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var vm = await BuildViewModelAsync(eventId);
                return View("~/Views/BudgetProposal/Detail.cshtml", vm);
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(Review)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
                return RedirectToAction("PendingApprovals", "Approver");
            }
        }

        // ─── Approve ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string proposalId, string eventId, string? note)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.ApproveAsync(CurrentUserId, proposalId, note);
                SetSuccess("Đã duyệt Budget Proposal thành công!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(Approve)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Review), new { eventId });
        }

        // ─── Reject ──────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string proposalId, string eventId, string note)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(note))
            {
                SetError("Vui lòng nhập lý do từ chối.");
                return RedirectToAction(nameof(Review), new { eventId });
            }

            try
            {
                await _service.RejectAsync(CurrentUserId, proposalId, note);
                SetSuccess("Đã từ chối Budget Proposal.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(Reject)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Review), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReceiptStatus(
    string receiptId, string eventId, ExpenseStatusEnum status)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.UpdateReceiptStatusAsync(CurrentUserId, receiptId, status);
                SetSuccess(status == ExpenseStatusEnum.Accepted
                    ? "Đã xác nhận biên lai."
                    : "Đã từ chối biên lai.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(UpdateReceiptStatus)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Review), new { eventId });
        }
    
    // Accepted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptReceipt(string receiptId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");
            try
            {
                await _service.UpdateReceiptStatusAsync(
                    CurrentUserId, receiptId, ExpenseStatusEnum.Accepted);
                SetSuccess("Đã xác nhận biên lai.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(AcceptReceipt)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }
            return RedirectToAction(nameof(Review), new { eventId });
        }

        // Rejected + gửi thông báo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReceipt(
            string receiptId, string eventId, string note)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(note))
            {
                SetError("Vui lòng nhập lý do từ chối.");
                return RedirectToAction(nameof(Review), new { eventId });
            }

            try
            {
                await _service.RejectReceiptAsync(CurrentUserId, receiptId, eventId, note);
                SetSuccess("Đã từ chối biên lai và thông báo cho Organizer.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetApprovalController)}.{nameof(RejectReceipt)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }
            return RedirectToAction(nameof(Review), new { eventId });
        }
    }
}
