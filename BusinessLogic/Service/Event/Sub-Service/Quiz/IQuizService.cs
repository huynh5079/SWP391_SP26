    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BusinessLogic.DTOs.Event.Quiz;

    namespace BusinessLogic.Service.Event.Sub_Service.Quiz
    {
        public interface IQuizService
        {
            //Add
		    Task<QuizDTO> AddQuizSetAsync(QuizDTO dto);
            Task<QuizQuestionDTO> AddQuestionQuizAsync(QuizQuestionDTO dto);
            //Read
		    Task<List<QuizDTO>> GetAllAsync();

            Task<QuizDTO?> GetByIdAsync(string quizId);

            Task<List<QuizDTO>> GetByEventIdAsync(string eventId);
		    Task<StudentQuizScoreDTO?> GetStudentScoreAsync(string quizId, string studentId);

		    Task<List<StudentQuizScoreDTO>> GetScoresByQuizAsync(string quizId);
		    Task<List<QuizQuestionDTO>> GetQuestionsAsync(string quizId);
		    //Update
		    Task<QuizDTO> UpdateQuizSetAsync(string quizsetId, QuizDTO dto);
            Task<QuizQuestionDTO> UpdateQuestionAsync(string questionId,QuizQuestionDTO dto, QuizDTO qdto);
		    //delete
		    Task<bool> DeleteAsync(QuizDTO dto,string quizId);
		    Task<bool> DeleteQuestionAsync(QuizDTO dto,string questionId);

		    // Upload a file associated with a quiz. Returns the stored file path or identifier.
		    Task<string> UploadFileAsync(string quizId, byte[] fileContent, string fileName);

            // Questions
        

            // Student submissions / scores
            // answers: map of questionId -> selectedAnswer (e.g., "A","B","C","D")
            Task<StudentQuizScoreDTO> SubmitQuizAsync(string quizId, string studentId, Dictionary<string, string> answers);

        
        }
    }
