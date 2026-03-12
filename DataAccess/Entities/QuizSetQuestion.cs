using System;

namespace DataAccess.Entities;

public partial class QuizSetQuestion : BaseEntity
{
    public string QuizSetId { get; set; } = null!;

    public string QuestionBankId { get; set; } = null!;

    public int? ScorePoint { get; set; }

    public int OrderIndex { get; set; }

    public virtual QuizSet QuizSet { get; set; } = null!;

    public virtual QuestionBank QuestionBank { get; set; } = null!;
}
