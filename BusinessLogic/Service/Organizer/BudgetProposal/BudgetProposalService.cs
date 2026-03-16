using BusinessLogic.DTOs.Organizer;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using static BusinessLogic.DTOs.Organizer.BudgetProposal.BugetProposalDtos;

namespace BusinessLogic.Service.Organizer.BudgetProposal
{
    public class BudgetProposalService : IBudgetProposalService
    {
        private readonly IUnitOfWork _uow;

        public BudgetProposalService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ─── Helper: kiểm tra Event tồn tại ──────────────────────────────────
        private async Task<DataAccess.Entities.Event> RequireEventAsync(string eventId)
        {
            var ev = await _uow.Events.GetByIdAsync(eventId);
            if (ev == null)
                throw new InvalidOperationException("Không tìm thấy sự kiện.");
            return ev;
        }

        // ─── Helper: kiểm tra Proposal tồn tại ───────────────────────────────
        private async Task<DataAccess.Entities.BudgetProposal> RequireProposalAsync(string proposalId)
        {
            var proposal = await _uow.BudgetProposals.GetAsync(
                p => p.Id == proposalId && p.DeletedAt == null,
                q => q.Include(p => p.BudgetItems)
                       .Include(p => p.ExpenseReceipts));
            if (proposal == null)
                throw new InvalidOperationException("Không tìm thấy Budget Proposal.");
            return proposal;
        }

        // ─── 1. Lấy Proposal theo Event ──────────────────────────────────────
        public async Task<BudgetProposalDetailDto> GetByEventAsync(string eventId)
        {
            var proposal = await _uow.BudgetProposals.GetAsync(
                p => p.EventId == eventId && p.DeletedAt == null,
                q => q.Include(p => p.BudgetItems)
                       .Include(p => p.ExpenseReceipts));

            if (proposal == null)
                return null; // thay vì throw

            return MapToDetailDto(proposal);
        }

    
        public async Task<BudgetProposalDetailDto> CreateAsync(string organizerId, CreateBudgetProposalDto dto)
        {
            await RequireEventAsync(dto.EventId);

            var existing = await _uow.BudgetProposals.GetAsync(
                p => p.EventId == dto.EventId &&
                     p.DeletedAt == null &&
                     p.Status != ProposalStatusEnum.Rejected);
            if (existing != null)
                throw new InvalidOperationException(
                    "Sự kiện này đã có Budget Proposal đang hoạt động.");

            var proposal = new DataAccess.Entities.BudgetProposal
            {
                EventId = dto.EventId,
                Title = dto.Title,
                Description = dto.Description,
                PlannedAmount = 0,
                Status = ProposalStatusEnum.Draft,  // ✅ Draft thay vì Pending
                CreatedBy = organizerId
            };

            await _uow.BudgetProposals.CreateAsync(proposal);
            await _uow.SaveChangesAsync();

            return MapToDetailDto(proposal);
        }

        public async Task SubmitForApprovalAsync(string organizerId, string proposalId)
        {
            var proposal = await RequireProposalAsync(proposalId);

            if (proposal.Status != ProposalStatusEnum.Draft)
                throw new InvalidOperationException(
                    "Chỉ có thể gửi duyệt Proposal đang ở trạng thái Draft.");

            if (!proposal.BudgetItems.Any(i => i.DeletedAt == null))
                throw new InvalidOperationException(
                    "Vui lòng thêm ít nhất 1 hạng mục trước khi gửi duyệt.");

            proposal.Status = ProposalStatusEnum.Pending;
            proposal.UpdatedBy = organizerId;

            await _uow.BudgetProposals.UpdateAsync(proposal);
            await _uow.SaveChangesAsync();
        }

        // ─── 3. Approver duyệt ───────────────────────────────────────────────
        public async Task ApproveAsync(string approverId, string proposalId, string? note)
        {
            var proposal = await RequireProposalAsync(proposalId);

            if (proposal.Status != ProposalStatusEnum.Pending)
                throw new InvalidOperationException(
                    "Chỉ có thể duyệt Proposal đang ở trạng thái Pending.");

            proposal.Status = ProposalStatusEnum.Approved;
            proposal.ApprovedBy = approverId;
            proposal.ApprovedAt = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
            proposal.Note = note;
            proposal.UpdatedBy = approverId;

            await _uow.BudgetProposals.UpdateAsync(proposal);
            await _uow.SaveChangesAsync();
        }

        // ─── 4. Approver từ chối ─────────────────────────────────────────────
        public async Task RejectAsync(string approverId, string proposalId, string note)
        {
            var proposal = await RequireProposalAsync(proposalId);

            if (proposal.Status != ProposalStatusEnum.Pending)
                throw new InvalidOperationException(
                    "Chỉ có thể từ chối Proposal đang ở trạng thái Pending.");

            proposal.Status = ProposalStatusEnum.Rejected;
            proposal.ApprovedBy = approverId;
            proposal.ApprovedAt = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
            proposal.Note = note;
            proposal.UpdatedBy = approverId;

            await _uow.BudgetProposals.UpdateAsync(proposal);
            await _uow.SaveChangesAsync();
        }

        // ─── 5. Thêm BudgetItem ───────────────────────────────────────────────
        public async Task<BudgetItemDto> AddItemAsync(string organizerId, string proposalId, CreateBudgetItemDto dto)
        {
            var proposal = await RequireProposalAsync(proposalId);

            if (proposal.Status == ProposalStatusEnum.Pending ||
                proposal.Status == ProposalStatusEnum.Approved)
                throw new InvalidOperationException(
                    "Chỉ có thể chỉnh sửa Proposal đang ở trạng thái Draft.");

            var item = new BudgetItem
            {
                BudgetProposalId = proposalId,
                Category = dto.Category,
                Description = dto.Description,
                EstimatedAmount = dto.EstimatedAmount,
                CreatedBy = organizerId
            };

            await _uow.BudgetItems.CreateAsync(item);
            await _uow.SaveChangesAsync();

            await RecalculatePlannedAmountAsync(proposal);
            await _uow.SaveChangesAsync();

            return MapToItemDto(item);
        }

        // ─── 6. Cập nhật BudgetItem ───────────────────────────────────────────
        public async Task UpdateItemAsync(string organizerId, string itemId, CreateBudgetItemDto dto)
        {
            var item = await _uow.BudgetItems.GetAsync(
                i => i.Id == itemId && i.DeletedAt == null);
            if (item == null)
                throw new InvalidOperationException("Không tìm thấy Budget Item.");

            var proposal = await RequireProposalAsync(item.BudgetProposalId);

            if (proposal.Status == ProposalStatusEnum.Pending ||
            proposal.Status == ProposalStatusEnum.Approved)
                throw new InvalidOperationException(
                    "Chỉ có thể chỉnh sửa Proposal đang ở trạng thái Draft.");

            item.Category = dto.Category;
            item.Description = dto.Description;
            item.EstimatedAmount = dto.EstimatedAmount;
            item.UpdatedBy = organizerId;

            await _uow.BudgetItems.UpdateAsync(item);
            await _uow.SaveChangesAsync();

            await RecalculatePlannedAmountAsync(proposal);
            await _uow.SaveChangesAsync();
        }

        // ─── 7. Xóa BudgetItem ────────────────────────────────────────────────
        public async Task RemoveItemAsync(string organizerId, string itemId)
        {
            var item = await _uow.BudgetItems.GetAsync(
                i => i.Id == itemId && i.DeletedAt == null);
            if (item == null)
                throw new InvalidOperationException("Không tìm thấy Budget Item.");

            var proposal = await RequireProposalAsync(item.BudgetProposalId);

            if (proposal.Status == ProposalStatusEnum.Pending ||
            proposal.Status == ProposalStatusEnum.Approved)
                throw new InvalidOperationException(
                    "Chỉ có thể chỉnh sửa Proposal đang ở trạng thái Draft.");

            item.DeletedAt = DataAccess.Helper.DateTimeHelper.GetVietnamTime();
            item.UpdatedBy = organizerId;
            await _uow.BudgetItems.UpdateAsync(item);
            await _uow.SaveChangesAsync();

            await RecalculatePlannedAmountAsync(proposal);
            await _uow.SaveChangesAsync();
        }

        // ─── 8. Thêm ExpenseReceipt ───────────────────────────────────────────
        public async Task<ExpenseReceiptDto> AddReceiptAsync(string organizerId, string proposalId, CreateExpenseReceiptDto dto)
        {
            var proposal = await RequireProposalAsync(proposalId);

            if (proposal.Status != ProposalStatusEnum.Approved)
                throw new InvalidOperationException(
                    "Chỉ có thể thêm Receipt khi Proposal đã được duyệt.");

            var receipt = new ExpenseReceipt
            {
                BudgetProposalId = proposalId,
                Title = dto.Title,
                ActualAmount = dto.ActualAmount,          // ✅ đúng tên field
                ReceiptImageUrl = dto.ReceiptImageUrl,
                SubmittedBy = organizerId,
                Status = ExpenseStatusEnum.Pending, // ✅ đúng enum value
                CreatedBy = organizerId
            };

            await _uow.ExpenseReceipts.CreateAsync(receipt);
            await _uow.SaveChangesAsync();

            return MapToReceiptDto(receipt);
        }

        // ─── 9. Lấy danh sách Receipt ────────────────────────────────────────
        public async Task<List<ExpenseReceiptDto>> GetReceiptsByProposalAsync(string proposalId)
        {
            var receipts = await _uow.ExpenseReceipts.GetAllAsync(
                r => r.BudgetProposalId == proposalId && r.DeletedAt == null);

            return receipts.Select(MapToReceiptDto).ToList();
        }

        // ─── 10. Báo cáo quyết toán ──────────────────────────────────────────
        public async Task<BudgetSummaryDto> GetSummaryAsync(string proposalId)
        {
            var proposal = await RequireProposalAsync(proposalId);

            var receipts = await _uow.ExpenseReceipts.GetAllAsync(
                r => r.BudgetProposalId == proposalId && r.DeletedAt == null);

            decimal actualTotal = receipts.Sum(r => r.ActualAmount); // ✅ đúng tên field

            var itemSummaries = proposal.BudgetItems
                .Where(i => i.DeletedAt == null)
                .Select(i =>
                {
                    decimal actual = receipts
                        .Where(r => r.Title == i.Category)
                        .Sum(r => r.ActualAmount);          // ✅ đúng tên field
                    return new BudgetItemSummaryDto
                    {
                        Category = i.Category,
                        EstimatedAmount = i.EstimatedAmount,
                        ActualAmount = actual,
                        Variance = i.EstimatedAmount - actual
                    };
                }).ToList();

            return new BudgetSummaryDto
            {
                PlannedAmount = proposal.PlannedAmount,
                ActualAmount = actualTotal,
                Variance = proposal.PlannedAmount - actualTotal,
                IsOverBudget = actualTotal > proposal.PlannedAmount,
                ItemSummaries = itemSummaries
            };
        }

        // ─── Private Helpers ─────────────────────────────────────────────────
        private async Task RecalculatePlannedAmountAsync(DataAccess.Entities.BudgetProposal proposal)
        {
            var items = await _uow.BudgetItems.GetAllAsync(
                i => i.BudgetProposalId == proposal.Id && i.DeletedAt == null);
            proposal.PlannedAmount = items.Sum(i => i.EstimatedAmount);
            await _uow.BudgetProposals.UpdateAsync(proposal);
        }

        private static BudgetProposalDetailDto MapToDetailDto(DataAccess.Entities.BudgetProposal p) => new()
        {
            ProposalId = p.Id,
            EventId = p.EventId,
            Title = p.Title,
            Description = p.Description,
            PlannedAmount = p.PlannedAmount,
            ActualAmount = p.ExpenseReceipts?
                .Where(r => r.DeletedAt == null)
                .Sum(r => r.ActualAmount) ?? 0,             // ✅ đúng tên field
            Variance = p.PlannedAmount - (p.ExpenseReceipts?
                .Where(r => r.DeletedAt == null)
                .Sum(r => r.ActualAmount) ?? 0),             // ✅ đúng tên field
            Status = p.Status?.ToString(),
            Note = p.Note,
            ApprovedBy = p.ApprovedBy,
            ApprovedAt = p.ApprovedAt,
            Items = p.BudgetItems?
                .Where(i => i.DeletedAt == null)
                .Select(MapToItemDto)
                .ToList() ?? new()
        };

        private static BudgetItemDto MapToItemDto(BudgetItem i) => new()
        {
            ItemId = i.Id,
            Category = i.Category,
            Description = i.Description,
            EstimatedAmount = i.EstimatedAmount
        };

        private static ExpenseReceiptDto MapToReceiptDto(ExpenseReceipt r) => new()
        {
            ReceiptId = r.Id,
            Title = r.Title,
            ActualAmount = r.ActualAmount,                  // ✅ đúng tên field
            ReceiptImageUrl = r.ReceiptImageUrl,
            SubmittedBy = r.SubmittedBy,
            Status = r.Status?.ToString(),
            CreatedAt = r.CreatedAt
        };
    }
}