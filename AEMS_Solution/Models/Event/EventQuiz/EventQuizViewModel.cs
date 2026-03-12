using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using BusinessLogic.DTOs.Event.Quiz;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace AEMS_Solution.Models.Event.EventQuiz
{
    public class EventQuizViewModel
    {
        // A list of quizzes for index/list views
        public List<QuizDTO> Quizzes { get; set; } = new();

        // The currently selected/edited quiz
        public QuizDTO? Quiz { get; set; }

        // Helper for creating/updating a single question
        public QuizQuestionDTO NewQuestion { get; set; } = new();

        // File uploaded for the quiz (bound from a form)
        public IFormFile? FileUpload { get; set; }

        // Answers when a student submits a quiz
        public List<QuizAnswerViewModel> Answers { get; set; } = new();

        // Scores for a quiz
        public List<StudentQuizScoreDTO> Scores { get; set; } = new();

        // Current student's score (if any)
        public StudentQuizScoreDTO? CurrentStudentScore { get; set; }

        // Convenience properties
        public string EventId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string QuizId => Quiz?.QuizsetId ?? string.Empty;

        // Dropdowns for create/edit pages
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Events { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Topics { get; set; } = new();
    }

    // ViewModel used to submit an answer for a question
    public class QuizAnswerViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        // The selected answer should match the option values used in DTO (e.g. "A", "B", ...)
        public string? SelectedAnswer { get; set; }
    }
}
