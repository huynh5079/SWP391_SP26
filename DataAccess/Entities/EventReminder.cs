using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventReminder : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? EventId { get; set; }

    public int? RemindBefore { get; set; }

    public string? MessageTemplate { get; set; }

    public bool? IsSent { get; set; }

    public virtual Event? Event { get; set; }
}
