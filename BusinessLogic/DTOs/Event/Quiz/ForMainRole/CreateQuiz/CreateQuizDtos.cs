using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz
{
    public class CreateQuizSetRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string? SourceQuizSetId { get; set; }
        public string? TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public QuizTypeEnum Type { get; set; }
        public int? PassingScore { get; set; }
        public string? FileQuiz { get; set; }
        public string? LiveQuizLink { get; set; }
        public bool AllowReview { get; set; }
        public int? MaxAttempts { get; set; }
        public QuizSetVisibilityEnum SharingStatus { get; set; } = QuizSetVisibilityEnum.Private;
    }

    public class CreateQuizSetResponseDto
    {
        public QuizSummaryContract Quiz { get; set; } = new();
    }

    public class GetAvailableQuizBanksRequestDto
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetAvailableQuizBanksResponseDto
    {
        public List<QuizBankSummaryContract> QuizBanks { get; set; } = new();
    }
}
