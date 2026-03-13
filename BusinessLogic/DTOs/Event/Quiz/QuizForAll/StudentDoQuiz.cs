using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;

namespace BusinessLogic.DTOs.Event.Quiz.QuizForAll
{
    public class StudentQuizSessionDto
    {
        public string StudentQuizScoreId { get; set; } = string.Empty;
        public QuizSummaryContract Quiz { get; set; } = new();
        public List<QuizQuestionContract> Questions { get; set; } = new();
        public QuizTimeLimitDto Limit { get; set; } = new();
    }
}
