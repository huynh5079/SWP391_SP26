using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class Feedback : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? EventId { get; set; }

    public string? StudentId { get; set; }

    public double? Rating { get; set; } //total rating

    public string? Comment { get; set; }

    public FeedbackStatusEnum Status { get; set; }

    public FeedBackRatingsEnum RatingEvent { get; set; }

    //public DateTime? CreatedAt { get; set; }

    public virtual Event? Event { get; set; }

    public virtual StudentProfile? Student { get; set; }
}
