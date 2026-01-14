using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Department : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<StaffProfile> StaffProfiles { get; set; } = new List<StaffProfile>();

    public virtual ICollection<StudentProfile> StudentProfiles { get; set; } = new List<StudentProfile>();
}
