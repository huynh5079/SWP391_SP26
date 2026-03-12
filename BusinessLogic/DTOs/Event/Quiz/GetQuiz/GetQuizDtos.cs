using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.Contracts;

namespace BusinessLogic.DTOs.Event.Quiz.GetQuiz
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
}
