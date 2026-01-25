using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class StaffProfile: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string? StaffCode { get; set; }

    public string? DepartmentId { get; set; }

    public string? Position { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ApprovalLog> ApprovalLogs { get; set; } = new List<ApprovalLog>();

    public virtual ICollection<CheckInHistory> CheckInHistories { get; set; } = new List<CheckInHistory>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual User? User { get; set; }
}
