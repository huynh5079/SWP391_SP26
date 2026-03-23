using System;
using System.Collections.Generic;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts
{
    public class QuizQuestionOptionContract
    {
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
    }

    public class QuizQuestionContract
    {
        public string QuestionBankId { get; set; } = string.Empty;
        public string EventQuizQuestionId { get; set; } = string.Empty;
        public string QuizSetQuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public QuizQuestionOptionContract Options { get; set; } = new();
        public QuestionTypeOptionEnum TypeOption { get; set; } = QuestionTypeOptionEnum.SingleChoice;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public int ScorePoint { get; set; }
        public int OrderIndex { get; set; }
        public QuestionDifficultyEnum Difficulty { get; set; } = QuestionDifficultyEnum.Medium;
    }

    public class QuizSummaryContract
    {
        public string EventQuizId { get; set; } = string.Empty;
        public string QuizSetId { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public QuizSetVisibilityEnum SharingStatus { get; set; } = QuizSetVisibilityEnum.Private;
        public string? TopicId { get; set; }
        public string? OrganizerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FileQuiz { get; set; }
        public string? LiveQuizLink { get; set; }
        public QuizTypeEnum Type { get; set; }
        public QuizStatusEnum Status { get; set; }
        public QuestionSetEnum QuestionSetStatus { get; set; }
        public int? PassingScore { get; set; }
        public int? TimeLimit { get; set; }
        public bool AllowReview { get; set; }
        public bool IsActive { get; set; }
        public int? MaxAttempts { get; set; }
        public int QuestionCount { get; set; }
        public int AttemptCount { get; set; }
        public int PassedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class QuizScoreContract
    {
        public string StudentQuizScoreId { get; set; } = string.Empty;
        public string EventQuizId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public StudentQuizScoreStatusEnum Status { get; set; }
    }

    public class QuizBankSummaryContract
    {
        public string QuizSetId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string? OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FileQuiz { get; set; }
        public QuizSetVisibilityEnum SharingStatus { get; set; } = QuizSetVisibilityEnum.Private;
        public QuizBankSourceTypeEnum SourceType { get; set; } = QuizBankSourceTypeEnum.Organizer;
        public int QuestionCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StudentAnswerContract
    {
        public string StudentAnswerId { get; set; } = string.Empty;
        public string StudentQuizScoreId { get; set; } = string.Empty;
        public string QuestionBankId { get; set; } = string.Empty;
        public string? SelectedAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public int ScoreEarned { get; set; }
    }
}
