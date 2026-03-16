using System.ComponentModel.DataAnnotations;
using DataAccess.Enum;
namespace AEMS_Solution.Models.Organizer.BudgetProposal
{
    public class BudgetProposalViewModel
    {
        public string EventId { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;

        // Thông tin Proposal
        public string ProposalId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Variance { get; set; }
        public ProposalStatusEnum? Status { get; set; }
        public string? Note { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Danh sách BudgetItems
        public List<BudgetItemViewModel> Items { get; set; } = new();

        // Danh sách ExpenseReceipts
        public List<ExpenseReceiptViewModel> Receipts { get; set; } = new();

        // Báo cáo quyết toán
        public List<BudgetItemSummaryViewModel> ItemSummaries { get; set; } = new();

        // Form thêm BudgetItem
        public CreateBudgetItemViewModel NewItem { get; set; } = new();

        // Form thêm ExpenseReceipt
        public CreateExpenseReceiptViewModel NewReceipt { get; set; } = new();

        // Phân biệt role — ẩn/hiện nút Approve/Reject vs Add Item/Receipt
        public bool IsApprover { get; set; } = false;

        // Helper properties cho View
        public bool HasProposal => !string.IsNullOrEmpty(ProposalId);
        public bool CanAddItem => HasProposal && Status != ProposalStatusEnum.Approved;
        public bool CanAddReceipt => HasProposal && Status == ProposalStatusEnum.Approved;
        public bool CanApprove => IsApprover && Status == ProposalStatusEnum.Pending;
        public bool IsOverBudget => ActualAmount > PlannedAmount;
    }

    // ─── BudgetItem ───────────────────────────────────────────────────────────
    public class BudgetItemViewModel
    {
        public string ItemId { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal EstimatedAmount { get; set; }
    }

    public class CreateBudgetItemViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn hạng mục")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 1,000đ")]
        public decimal EstimatedAmount { get; set; }
    }

    // ─── ExpenseReceipt ───────────────────────────────────────────────────────
    public class ExpenseReceiptViewModel
    {
        public string ReceiptId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public decimal ActualAmount { get; set; }
        public string? ReceiptImageUrl { get; set; }
        public string? SubmittedBy { get; set; }
        public ExpenseStatusEnum? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseReceiptViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khoản chi")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền thực tế")]
        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 1,000đ")]
        public decimal ActualAmount { get; set; }

        public string? ReceiptImageUrl { get; set; }
    }

    // ─── Summary ──────────────────────────────────────────────────────────────
    public class BudgetItemSummaryViewModel
    {
        public string? Category { get; set; }
        public decimal EstimatedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Variance { get; set; }
        public bool IsOverBudget => ActualAmount > EstimatedAmount;
    }

    // ─── Form tạo Proposal mới ────────────────────────────────────────────────
    public class CreateBudgetProposalViewModel
    {
        public string EventId { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string? Title { get; set; }

        public string? Description { get; set; }
    }

}
