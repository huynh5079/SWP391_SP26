using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.Contracts;

namespace BusinessLogic.DTOs.Event.Quiz.GetQuizScores
{
    public class GetQuizScoresRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
    }

    public class GetQuizScoresResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public List<QuizScoreContract> Scores { get; set; } = new();
    }

    public class GetStudentQuizScoreRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class GetStudentQuizScoreResponseDto
    {
        public string QuizId { get; set; } = string.Empty;
        public QuizScoreContract? Score { get; set; }
    }
}
