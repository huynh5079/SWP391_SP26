using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class BudgetProposal : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal PlannedAmount { get; set; }

    public decimal? ActualAmount { get; set; }

    public ProposalStatusEnum? Status { get; set; }

    public string? Note { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<ExpenseReceipt> ExpenseReceipts { get; set; } = new List<ExpenseReceipt>();
}
