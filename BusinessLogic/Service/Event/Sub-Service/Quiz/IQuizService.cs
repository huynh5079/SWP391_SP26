using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
    public interface IQuizService
    {
        Task<List<QuizDTO>> GetAllAsync();

        Task<QuizDTO?> GetByIdAsync(string quizId);

        Task<List<QuizDTO>> GetByEventIdAsync(string eventId);

        Task<QuizDTO> CreateAsync(QuizDTO dto);

        Task<bool> UpdateAsync(string quizId, QuizDTO dto);

        Task<bool> DeleteAsync(string quizId);

        // Upload a file associated with a quiz. Returns the stored file path or identifier.
        Task<string> UploadFileAsync(string quizId, byte[] fileContent, string fileName);

        // Questions
        Task<List<QuizQuestionDTO>> GetQuestionsAsync(string quizId);

        Task<QuizQuestionDTO> AddQuestionAsync(string quizId, QuizQuestionDTO dto);

        Task<bool> UpdateQuestionAsync(string questionId, QuizQuestionDTO dto);

        Task<bool> DeleteQuestionAsync(string questionId);

        // Student submissions / scores
        // answers: map of questionId -> selectedAnswer (e.g., "A","B","C","D")
        Task<StudentQuizScoreDTO> SubmitQuizAsync(string quizId, string studentId, Dictionary<string, string> answers);

        Task<StudentQuizScoreDTO?> GetStudentScoreAsync(string quizId, string studentId);

        Task<List<StudentQuizScoreDTO>> GetScoresByQuizAsync(string quizId);
    }
}
