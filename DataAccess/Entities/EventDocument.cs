using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventDocument : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Url { get; set; }

    public string? Type { get; set; }

    public virtual Event Event { get; set; } = null!;
}
