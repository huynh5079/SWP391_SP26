using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class EventQuiz : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string EventId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Type { get; set; }

    public bool IsActive { get; set; }

    public int? PassingScore { get; set; }

    //public DateTime? CreatedAt { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public virtual ICollection<StudentQuizScore> StudentQuizScores { get; set; } = new List<StudentQuizScore>();
}
