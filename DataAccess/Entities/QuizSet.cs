using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class QuizSet : BaseEntity
{
    public string? TopicId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual Topic? Topic { get; set; }

    public virtual ICollection<EventQuiz> EventQuizzes { get; set; } = new List<EventQuiz>();

    public virtual ICollection<QuizSetQuestion> QuizSetQuestions { get; set; } = new List<QuizSetQuestion>();
}
