using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class ExpenseReceipt : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string BudgetProposalId { get; set; } = null!;

    public string? Title { get; set; }

    public decimal ActualAmount { get; set; }

    public string? ReceiptImageUrl { get; set; }

    public string? SubmittedBy { get; set; }

    //public DateTime? SubmittedAt { get; set; }

    public ExpenseStatusEnum? Status { get; set; }

    public virtual BudgetProposal BudgetProposal { get; set; } = null!;
}
