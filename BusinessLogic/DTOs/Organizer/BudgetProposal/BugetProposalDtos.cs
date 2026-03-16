using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Organizer.BudgetProposal
{
    public class BugetProposalDtos
    { // ─── BudgetProposal ───────────────────────────────
        public class CreateBudgetProposalDto
        {
            public string EventId { get; set; } = null!;
            public string? Title { get; set; }
            public string? Description { get; set; }
        }

        public class BudgetProposalDetailDto
        {
            public string ProposalId { get; set; } = null!;
            public string EventId { get; set; } = null!;
            public string? Title { get; set; }
            public string? Description { get; set; }
            public decimal PlannedAmount { get; set; }
            public decimal ActualAmount { get; set; }
            public decimal Variance { get; set; }
            public string? Status { get; set; }
            public string? Note { get; set; }
            public string? ApprovedBy { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public List<BudgetItemDto> Items { get; set; } = new();
        }

        // ─── BudgetItem ───────────────────────────────────
        public class CreateBudgetItemDto
        {
            public string? Category { get; set; }
            public string? Description { get; set; }
            public decimal EstimatedAmount { get; set; }
        }

        public class BudgetItemDto
        {
            public string ItemId { get; set; } = null!;
            public string? Category { get; set; }
            public string? Description { get; set; }
            public decimal EstimatedAmount { get; set; }
        }

        // ─── ExpenseReceipt ───────────────────────────────
        public class CreateExpenseReceiptDto
        {
            public string? Title { get; set; }
            public decimal ActualAmount { get; set; }
            public string? ReceiptImageUrl { get; set; }
        }

        public class ExpenseReceiptDto
        {
            public string ReceiptId { get; set; } = null!;
            public string? Title { get; set; }
            public decimal ActualAmount { get; set; }
            public string? ReceiptImageUrl { get; set; }
            public string? SubmittedBy { get; set; }
            public string? Status { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // ─── Summary ──────────────────────────────────────
        public class BudgetSummaryDto
        {
            public decimal PlannedAmount { get; set; }
            public decimal ActualAmount { get; set; }
            public decimal Variance { get; set; }
            public bool IsOverBudget { get; set; }
            public List<BudgetItemSummaryDto> ItemSummaries { get; set; } = new();
        }

        public class BudgetItemSummaryDto
        {
            public string? Category { get; set; }
            public decimal EstimatedAmount { get; set; }
            public decimal ActualAmount { get; set; }
            public decimal Variance { get; set; }
        }
    } 
}
