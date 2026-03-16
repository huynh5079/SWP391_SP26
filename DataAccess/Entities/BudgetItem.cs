using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities
{
    public partial class BudgetItem : BaseEntity
    {
        public string BudgetProposalId { get; set; } = null!;
        public string? Category { get; set; }
        public string? Description { get; set; }
        public decimal EstimatedAmount { get; set; }

        // Navigation
        public virtual BudgetProposal BudgetProposal { get; set; } = null!;
    }
}
