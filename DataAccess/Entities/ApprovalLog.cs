using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class ApprovalLog : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? ApproverId { get; set; }

    public ApprovalActionEnum? Action { get; set; }

    public string? Comment { get; set; }

    //public DateTime? LogDate { get; set; }

    public virtual StaffProfile? Approver { get; set; }

    public virtual Event Event { get; set; } = null!;
}
