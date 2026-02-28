using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Ticket: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? TicketCode { get; set; }

    public string EventId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    public TicketStatusEnum Status { get; set; }

    //public DateTime? RegisteredAt { get; set; }

    public DateTime? CheckInTime { get; set; }

    public virtual ICollection<CheckInHistory> CheckInHistories { get; set; } = new List<CheckInHistory>();

    public virtual Event Event { get; set; } = null!;

    public virtual StudentProfile Student { get; set; } = null!;
}
