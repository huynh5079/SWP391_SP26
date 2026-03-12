using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz
{
    public class GetQuizDetailRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
    }

    public class GetQuizDetailResponseDto
    {
        public QuizSummaryContract Quiz { get; set; } = new();
        public List<QuizQuestionContract> Questions { get; set; } = new();
    }

    public class GetOrganizerQuizzesRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? EventId { get; set; }
    }

    public class GetOrganizerQuizzesResponseDto
    {
        public List<QuizSummaryContract> Quizzes { get; set; } = new();
    }
}
