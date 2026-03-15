using DataAccess.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities;

public partial class BudgetProposal : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal PlannedAmount { get; set; }

    public ProposalStatusEnum? Status { get; set; }

    public string? Note { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    // thêm approval 
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }


    public virtual Event Event { get; set; } = null!;
    public virtual User? Approver { get; set; }
    public virtual ICollection<ExpenseReceipt> ExpenseReceipts
    { get; set; } = new List<ExpenseReceipt>();

    // Computed — không lưu vào DB
    [NotMapped]
    public decimal ActualAmount =>
        ExpenseReceipts?.Sum(r => r.ActualAmount) ?? 0;

    [NotMapped]
    public decimal Variance => PlannedAmount - ActualAmount;
}
