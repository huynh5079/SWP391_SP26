using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class Feedback : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? EventId { get; set; }

    public string? StudentId { get; set; }

    

    public string? Comment { get; set; }

    public FeedbackStatusEnum Status { get; set; }

    public FeedBackRatingsEnum? RatingEvent { get; set; } //Label

    //public DateTime? CreatedAt { get; set; }

    public virtual Event? Event { get; set; }

    public virtual StudentProfile? Student { get; set; }

    public int? Label { get; set; }
    public int? Technical { get; set; }
    public int? Content { get; set; }
    public int? Instructor { get; set; }
    public int? Assessment { get; set; }
    public string? Label_Text { get; set; }
    public string? Technical_Text { get; set; }
    public string? Content_Text { get; set; }
    public string? Instructor_Text { get; set; }
    public string? Assessment_Text { get; set; }
}
