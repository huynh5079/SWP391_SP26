using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventWaitlist : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    public DateTime? JoinedAt { get; set; }

    public bool? IsNotified { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual StudentProfile Student { get; set; } = null!;
}
