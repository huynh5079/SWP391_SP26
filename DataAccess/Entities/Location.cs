using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class Location : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public int Capacity { get; set; }

    public LocationStatusEnum Status { get; set; } 

    public LocationTypeEnum? Type { get; set; }

    public string? Description { get; set; }

    // Navigation property
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
