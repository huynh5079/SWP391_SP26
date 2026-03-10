using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class QuizQuestion : BaseEntity
{
    //public string Id { get; set; } = null!;

    public string QuizId { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    public string OptionA { get; set; } = null!;

    public string OptionB { get; set; } = null!;

    public string? OptionC { get; set; }

    public string? OptionD { get; set; }

    public string CorrectAnswer { get; set; } = null!;

    public int? ScorePoint { get; set; }

    public string? FileQuiz { get; set; }

    public virtual EventQuiz Quiz { get; set; } = null!;
}
