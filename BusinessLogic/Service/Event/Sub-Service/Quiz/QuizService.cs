using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
	public class QuizService : IQuizService
	{
		private readonly IUnitOfWork _uow;
		public QuizService(IUnitOfWork uow) {
		  _uow = uow;
		}
		public Task<QuizQuestionDTO> AddQuestionAsync(string quizId, QuizQuestionDTO dto)
		{
			throw new NotImplementedException();
		}

		public Task<QuizDTO> CreateAsync(QuizDTO dto)
		{
			throw new NotImplementedException();
		}

		public Task<bool> DeleteAsync(string quizId)
		{
			throw new NotImplementedException();
		}

		public Task<string> UploadFileAsync(string quizId, byte[] fileContent, string fileName)
		{
			throw new NotImplementedException();
		}

		public Task<bool> DeleteQuestionAsync(string questionId)
		{
			throw new NotImplementedException();
		}

		public Task<List<QuizDTO>> GetAllAsync()
		{
			throw new NotImplementedException();
		}

		public Task<List<QuizDTO>> GetByEventIdAsync(string eventId)
		{
			throw new NotImplementedException();
		}

		public Task<QuizDTO?> GetByIdAsync(string quizId)
		{
			throw new NotImplementedException();
		}

		public Task<List<QuizQuestionDTO>> GetQuestionsAsync(string quizId)
		{
			throw new NotImplementedException();
		}

		public Task<List<StudentQuizScoreDTO>> GetScoresByQuizAsync(string quizId)
		{
			throw new NotImplementedException();
		}

		public Task<StudentQuizScoreDTO?> GetStudentScoreAsync(string quizId, string studentId)
		{
			throw new NotImplementedException();
		}

		public Task<StudentQuizScoreDTO> SubmitQuizAsync(string quizId, string studentId, Dictionary<string, string> answers)
		{
			throw new NotImplementedException();
		}

		public Task<bool> UpdateAsync(string quizId, QuizDTO dto)
		{
			throw new NotImplementedException();
		}

		public Task<bool> UpdateQuestionAsync(string questionId, QuizQuestionDTO dto)
		{
			throw new NotImplementedException();
		}
	}
}
