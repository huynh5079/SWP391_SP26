using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion
{
    public class AddQuizQuestionRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public QuestionTypeOptionEnum TypeOption { get; set; } = QuestionTypeOptionEnum.SingleChoice;
        public string QuestionText { get; set; } = string.Empty;
        public QuizQuestionOptionContract Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int ScorePoint { get; set; } = 1;
        public QuestionDifficultyEnum Difficulty { get; set; } = QuestionDifficultyEnum.Medium;
    }

    public class AddQuizQuestionResponseDto
    {
        public QuizQuestionContract Question { get; set; } = new();
    }
}
