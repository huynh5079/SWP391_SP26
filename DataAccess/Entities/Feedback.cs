using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Feedback : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? EventId { get; set; }

    public string? StudentId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    //public DateTime? CreatedAt { get; set; }

    public virtual Event? Event { get; set; }

    public virtual StudentProfile? Student { get; set; }
}
