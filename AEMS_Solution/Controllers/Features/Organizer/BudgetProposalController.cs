using AEMS_Solution.Controllers.Common;
using AEMS_Solution.Models.Organizer.BudgetProposal;
using BusinessLogic.Service.Organizer.BudgetProposal;
using BusinessLogic.Service.System;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static BusinessLogic.DTOs.Organizer.BudgetProposal.BugetProposalDtos;

namespace AEMS_Solution.Controllers.Features.Organizer
{
    [Authorize(Roles = "Organizer")]
    public class BudgetProposalController : BaseController
    {
        private readonly IBudgetProposalService _service;
        private readonly ISystemErrorLogService _errorLog;
        private readonly ILogger<BudgetProposalController> _logger;

        public BudgetProposalController(
            IBudgetProposalService service,
            ISystemErrorLogService errorLog,
            ILogger<BudgetProposalController> logger)
        {
            _service = service;
            _errorLog = errorLog;
            _logger = logger;
        }

        // ─── Helper: build ViewModel từ eventId ──────────────────────────────
        private async Task<BudgetProposalViewModel> BuildViewModelAsync(string eventId, bool isApprover = false)
        {
            var vm = new BudgetProposalViewModel
            {
                EventId = eventId,
                IsApprover = isApprover
            };

            try
            {
                var proposal = await _service.GetByEventAsync(eventId);
                if (proposal == null) return vm; // trả về vm rỗng luôn

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

                vm.Receipts = receipts.Select(r => new ExpenseReceiptViewModel
                {
                    ReceiptId = r.ReceiptId,
                    Title = r.Title,
                    ActualAmount = r.ActualAmount,
                    ReceiptImageUrl = r.ReceiptImageUrl,
                    SubmittedBy = r.SubmittedBy,
                    CreatedAt = r.CreatedAt
                }).ToList();

                if (Enum.TryParse<ExpenseStatusEnum>(receipts.FirstOrDefault()?.Status, out _))
                {
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
                }

                if (summary != null)
                {
                    vm.ItemSummaries = summary.ItemSummaries?.Select(s => new BudgetItemSummaryViewModel
                    {
                        Category = s.Category,
                        EstimatedAmount = s.EstimatedAmount,
                        ActualAmount = s.ActualAmount,
                        Variance = s.Variance
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                // Tạm thời log để debug
                _logger.LogError(ex, "BuildViewModelAsync failed for eventId={EventId}", eventId);
            }

            return vm;
        }

        // ─── 1. Detail ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Detail(string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var vm = await BuildViewModelAsync(eventId, isApprover: false);
                return View("~/Views/BudgetProposal/Detail.cshtml", vm);
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(Detail)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
                return RedirectToAction("Manage", "Organizer", new { operation = "myevents" });
            }
        }

        // ─── 2. Tạo Proposal ─────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBudgetProposalDto dto)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Detail), new { eventId = dto.EventId });
            }

            try
            {
                await _service.CreateAsync(CurrentUserId, dto);
                SetSuccess("Tạo Budget Proposal thành công!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(Create)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId = dto.EventId });
        }
        // ─── Edit Proposal ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProposal(string proposalId, string eventId,
            string? Title, string? Description)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(Title))
            {
                SetError("Tiêu đề không được để trống.");
                return RedirectToAction(nameof(Detail), new { eventId });
            }

            try
            {
                await _service.EditProposalAsync(CurrentUserId, proposalId, Title, Description);
                SetSuccess("Đã cập nhật Proposal. Bạn có thể thêm hạng mục và gửi duyệt lại!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(EditProposal)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }

        // ─── Delete Proposal ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProposal(string proposalId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.DeleteProposalAsync(CurrentUserId, proposalId);
                SetSuccess("Đã xóa Budget Proposal thành công.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(DeleteProposal)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            // Sau khi xóa → quay về MyEvents vì Proposal không còn tồn tại
            return RedirectToAction("Manage", "Organizer", new { operation = "myevents" });
        }

        // ─── 3. Thêm BudgetItem ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(string proposalId, string eventId, CreateBudgetItemDto dto)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Detail), new { eventId });
            }

            try
            {
                await _service.AddItemAsync(CurrentUserId, proposalId, dto);
                SetSuccess("Thêm hạng mục thành công!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(AddItem)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }

        // ─── Submit for Approval ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForApproval(string proposalId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.SubmitForApprovalAsync(CurrentUserId, proposalId);
                SetSuccess("Đã gửi Budget Proposal lên Approver duyệt!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(SubmitForApproval)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }

        // ─── 4. Cập nhật BudgetItem ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(string itemId, string eventId, CreateBudgetItemDto dto)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Detail), new { eventId });
            }

            try
            {
                await _service.UpdateItemAsync(CurrentUserId, itemId, dto);
                SetSuccess("Cập nhật hạng mục thành công!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(UpdateItem)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }

        // ─── 5. Xóa BudgetItem ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(string itemId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            try
            {
                await _service.RemoveItemAsync(CurrentUserId, itemId);
                SetSuccess("Đã xóa hạng mục.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(RemoveItem)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }

        // ─── 6. Thêm ExpenseReceipt ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReceipt(string proposalId, string eventId, CreateExpenseReceiptDto dto)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                SetError("Dữ liệu không hợp lệ.");
                return RedirectToAction(nameof(Detail), new { eventId });
            }

            try
            {
                await _service.AddReceiptAsync(CurrentUserId, proposalId, dto);
                SetSuccess("Thêm biên lai chi phí thành công!");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(AddReceipt)}");

                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }

            return RedirectToAction(nameof(Detail), new { eventId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceipt(
        string receiptId, string eventId, string? Title, decimal ActualAmount)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");
            try
            {
                await _service.EditReceiptAsync(CurrentUserId, receiptId, Title, ActualAmount);
                SetSuccess("Đã cập nhật biên lai. Trạng thái chuyển về Pending.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(EditReceipt)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }
            return RedirectToAction(nameof(Detail), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReceipt(string receiptId, string eventId)
        {
            if (CurrentUserId == null) return RedirectToAction("Login", "Auth");
            try
            {
                await _service.DeleteReceiptAsync(CurrentUserId, receiptId);
                SetSuccess("Đã xóa biên lai.");
            }
            catch (Exception ex)
            {
                await _errorLog.LogErrorAsync(ex, CurrentUserId,
                    $"{nameof(BudgetProposalController)}.{nameof(DeleteReceipt)}");
                var deepest = ex;
                while (deepest.InnerException != null) deepest = deepest.InnerException;
                SetError(deepest.Message);
            }
            return RedirectToAction(nameof(Detail), new { eventId });
        }
    }
}
