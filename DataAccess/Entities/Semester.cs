using DataAccess.Enum;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Semester: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public string? Code { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SemesterStatusEnum Status { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
