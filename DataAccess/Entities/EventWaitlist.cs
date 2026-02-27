using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class EventWaitlist : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    // When the student joined the waitlist
    public DateTime? JoinedAt { get; set; }

    // Whether the student has been notified about an available slot
    public bool? IsNotified { get; set; }

    // Current status of the waitlist entry
    public EventWaitlistStatusEnum? Status { get; set; }

    // When an offer was sent to the student (if any)
    public DateTime? OfferedAt { get; set; }

    // When the student responded to the offer (accepted/declined)
    public DateTime? RespondedAt { get; set; }

    // Position in the waitlist (1 = first in line)
    public int? Position { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual StudentProfile Student { get; set; } = null!;

}
