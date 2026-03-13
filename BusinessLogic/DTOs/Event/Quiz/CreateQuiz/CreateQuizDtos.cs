using BusinessLogic.DTOs.Event.Quiz.Contracts;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.CreateQuiz
{
    public class CreateQuizSetRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public QuizTypeEnum Type { get; set; }
        public int? PassingScore { get; set; }
        public string? FileQuiz { get; set; }
        public string? LiveQuizLink { get; set; }
    }

    public class CreateQuizSetResponseDto
    {
        public QuizSummaryContract Quiz { get; set; } = new();
    }
}
