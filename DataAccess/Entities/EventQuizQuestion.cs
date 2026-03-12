using DataAccess.Enum;

namespace DataAccess.Entities;

public partial class EventQuizQuestion : BaseEntity
{
	public string EventQuizId { get; set; } = null!;

	public string? QuestionBankId { get; set; }

	public string QuestionText { get; set; } = null!;

	public string OptionA { get; set; } = null!;

	public string OptionB { get; set; } = null!;

	public string? OptionC { get; set; }

	public string? OptionD { get; set; }

	public string CorrectAnswer { get; set; } = null!;

	public QuestionDifficultyEnum Difficulty { get; set; } = QuestionDifficultyEnum.Medium;

	public int ScorePoint { get; set; } = 1;

	public int OrderIndex { get; set; }

	public virtual EventQuiz EventQuiz { get; set; } = null!;

	public virtual QuestionBank? QuestionBank { get; set; }
}
