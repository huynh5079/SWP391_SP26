using System;
using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;

namespace BusinessLogic.DTOs.Event.Quiz.QuizForAll
{
	public class StartQuizRequestDto
	{
		public string QuizId { get; set; } = string.Empty;
		public string StudentId { get; set; } = string.Empty;
	}

	public class StartQuizResponseDto
	{
		public StudentQuizSessionDto Session { get; set; } = new();
	}

    public class GetCurrentQuizSessionRequestDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class GetCurrentQuizSessionResponseDto
    {
        public StudentQuizSessionDto? Session { get; set; }
    }

	public class SubmitQuizAnswerDto
	{
		public string QuestionBankId { get; set; } = string.Empty;
		public string? SelectedAnswer { get; set; }
	}

	public class SubmitQuizRequestDto
	{
		public string QuizId { get; set; } = string.Empty;
		public string StudentId { get; set; } = string.Empty;
		public string? StudentQuizScoreId { get; set; }
		public List<SubmitQuizAnswerDto> Answers { get; set; } = new();
	}

	public class SubmitQuizResponseDto
	{
		public string QuizId { get; set; } = string.Empty;
		public string StudentQuizScoreId { get; set; } = string.Empty;
		public int Score { get; set; }
		public bool IsPassed { get; set; }
		public bool AllowReview { get; set; }
		public bool IsTimedOut { get; set; }
		public DateTime SubmittedAt { get; set; }
		public List<StudentAnswerContract> Answers { get; set; } = new();
		public List<QuizQuestionContract> ReviewQuestions { get; set; } = new();
	}
}
