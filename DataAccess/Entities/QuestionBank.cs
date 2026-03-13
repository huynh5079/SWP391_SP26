using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class QuestionBank : BaseEntity
{
    public string? TopicId { get; set; }

    public string? OrganizerId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string OptionA { get; set; } = null!;

    public string OptionB { get; set; } = null!;

    public string? OptionC { get; set; }

    public string? OptionD { get; set; }

    public string CorrectAnswer { get; set; } = null!;

    public string? Explanation { get; set; }

    public QuestionDifficultyEnum Difficulty { get; set; } = QuestionDifficultyEnum.Medium;

    public virtual Topic? Topic { get; set; }

    public virtual StaffProfile? Organizer { get; set; }

    public virtual ICollection<EventQuizQuestion> EventQuizQuestions { get; set; } = new List<EventQuizQuestion>();

    public virtual ICollection<QuizSetQuestion> QuizSetQuestions { get; set; } = new List<QuizSetQuestion>();

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}