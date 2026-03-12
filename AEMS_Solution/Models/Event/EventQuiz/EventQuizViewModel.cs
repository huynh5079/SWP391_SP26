using System;
using System.Collections.Generic;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace AEMS_Solution.Models.Event.EventQuiz
{
    public class EventQuizViewModel
    {
        // A list of quizzes for index/list views
        public List<QuizSummaryContract> Quizzes { get; set; } = new();

        // The currently selected/edited quiz
        public QuizSummaryContract Quiz { get; set; } = new();

        public GetQuizDetailResponseDto? Detail { get; set; }

        public List<QuizQuestionContract> Questions { get; set; } = new();

        // Helper for creating/updating a single question
        public AddQuizQuestionRequestDto NewQuestion { get; set; } = new();

        public List<AddQuizQuestionRequestDto> ManualQuestions { get; set; } = new()
        {
            new AddQuizQuestionRequestDto()
        };

        // File uploaded for the quiz (bound from a form)
        public IFormFile? FileUpload { get; set; }

        // Answers when a student submits a quiz
        public List<QuizAnswerViewModel> Answers { get; set; } = new();

        // Scores for a quiz
        public List<QuizScoreContract> Scores { get; set; } = new();

        // Current student's score (if any)
        public QuizScoreContract? CurrentStudentScore { get; set; }

        // Convenience properties
        public string EventId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string SelectedQuizSetId { get; set; } = string.Empty;
        public string QuizBankSourceFilter { get; set; } = "All";
        public string QuizId => Quiz.EventQuizId;
        public string EventTitle { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;

        // Dropdowns for create/edit pages
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Events { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Topics { get; set; } = new();
        public List<QuizBankSummaryContract> AvailableQuizBanks { get; set; } = new();
    }

    // ViewModel used to submit an answer for a question
    public class QuizAnswerViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        // The selected answer should match the option values used in DTO (e.g. "A", "B", ...)
        public string? SelectedAnswer { get; set; }
    }
}
