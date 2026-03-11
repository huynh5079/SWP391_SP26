using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz
{
    public class QuizDTO
    {
		public string QuizsetId { get; set; } = string.Empty;

		public string EventId { get; set; } = string.Empty;

		public string Title { get; set; } = string.Empty;

		public QuizTypeEnum Type { get; set; }

		public int? PassingScore { get; set; }

		public QuestionSetEnum QuestionSetStatus { get; set; } = QuestionSetEnum.NA;

		public string? FileQuiz { get; set; }

		public List<QuizQuestionDTO> Questions { get; set; } = new();

		public int QuestionCount => Questions?.Count ?? 0;

		public int AttemptCount { get; set; }

		public int PassedCount { get; set; }
        public QuizStatusEnum Status { get; set; }
	}

    public class QuizQuestionDTO
    {
        public string Id { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; }
        public int? ScorePoint { get; set; }
    }
	//student score for quiz
	public class StudentQuizScoreDTO
    {
        public string Id { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public bool IsPassed { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
