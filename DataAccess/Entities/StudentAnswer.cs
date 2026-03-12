using System;

namespace DataAccess.Entities;

public partial class StudentAnswer : BaseEntity
{
    public string StudentQuizScoreId { get; set; } = null!;

    public string QuestionBankId { get; set; } = null!;

    public string? SelectedAnswer { get; set; }

    public bool IsCorrect { get; set; }

    public int ScoreEarned { get; set; }

    public virtual StudentQuizScore StudentQuizScore { get; set; } = null!;

    public virtual QuestionBank QuestionBank { get; set; } = null!;
}
