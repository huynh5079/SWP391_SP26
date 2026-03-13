using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz
{
    public class UpdateQuizSetRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public QuizTypeEnum Type { get; set; }
        public int? PassingScore { get; set; }
        public int? TimeLimit { get; set; }
        public string? FileQuiz { get; set; }
        public string? LiveQuizLink { get; set; }
        public bool AllowReview { get; set; }
    }

    public class UpdateQuizSetResponseDto
    {
        public QuizSummaryContract Quiz { get; set; } = new();
    }
}
