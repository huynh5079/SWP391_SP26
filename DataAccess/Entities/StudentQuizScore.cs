using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class StudentQuizScore: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string? EventQuizId { get; set; }

    public string StudentId { get; set; } = null!;

    public int? Score { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public StudentQuizScoreStatusEnum Status { get; set; }

    //public DateTime? SubmittedAt { get; set; }

    public virtual EventQuiz? EventQuiz { get; set; }

    public virtual StudentProfile Student { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
