using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class StudentQuizScore: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string QuizId { get; set; } = null!;

    public string StudentId { get; set; } = null!;

    public int TotalScore { get; set; }

    public bool IsPassed { get; set; }

    //public DateTime? SubmittedAt { get; set; }

    public virtual EventQuiz Quiz { get; set; } = null!;

    public virtual StudentProfile Student { get; set; } = null!;
}
