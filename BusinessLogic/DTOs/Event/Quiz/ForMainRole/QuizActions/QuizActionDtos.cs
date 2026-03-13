using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.QuizActions
{
    public class PublishQuizRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    public class PublishQuizResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public QuizStatusEnum Status { get; set; }
    }

    public class PublishQuizSetRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public QuizSetVisibilityEnum SharingStatus { get; set; } = QuizSetVisibilityEnum.Public;
    }

    public class PublishQuizSetResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public QuizSetVisibilityEnum SharingStatus { get; set; }
    }

    public class CloseQuizRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class CloseQuizResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public QuizStatusEnum Status { get; set; }
    }

    public class PreviewQuizRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    public class PreviewQuizResponseDto
    {
        public GetQuizDetailResponseDto Preview { get; set; } = new();
    }

    public class UpdateQuizQuestionRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string EventQuizQuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public QuizQuestionOptionContract Options { get; set; } = new();
        public QuestionTypeOptionEnum TypeOption { get; set; } = QuestionTypeOptionEnum.SingleChoice;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public int ScorePoint { get; set; } = 1;
        public QuestionDifficultyEnum Difficulty { get; set; } = QuestionDifficultyEnum.Medium;
    }

    public class UpdateQuizQuestionResponseDto
    {
        public QuizQuestionContract Question { get; set; } = new();
    }

    public class DeleteQuizQuestionRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string EventQuizQuestionId { get; set; } = string.Empty;
    }

    public class DeleteQuizQuestionResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string EventQuizQuestionId { get; set; } = string.Empty;
        public int RemainingQuestions { get; set; }
    }

    public class DeleteQuizRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class DeleteQuizResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
    }
}
