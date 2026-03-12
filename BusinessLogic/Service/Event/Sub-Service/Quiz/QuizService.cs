using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz;
using BusinessLogic.Service.ValidationData.Quiz;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories;
using DataAccess.Repositories.Abstraction;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
	public class QuizService : IQuizService
	{
		private readonly IUnitOfWork _uow;
		private readonly IQuizValidator _validator;
		public QuizService(IUnitOfWork uow,IQuizValidator validator) {
		  _uow = uow;
		  _validator = validator;
		}

		private static QuizQuestionOptionDTO GetQuestionOptions(QuizQuestionDTO dto)
		{
			return dto.Option?.FirstOrDefault() ?? new QuizQuestionOptionDTO();
		}

		private static QuizQuestionDTO MapQuestionToDto(QuizQuestion question)
		{
			return new QuizQuestionDTO
			{
				Id = question.Id,
				QuizId = question.QuizId,
				QuestionText = question.QuestionText,
				Option = new List<QuizQuestionOptionDTO>
				{
					new QuizQuestionOptionDTO
					{
						OptionA = question.OptionA,
						OptionB = question.OptionB,
						OptionC = question.OptionC,
						OptionD = question.OptionD
					}
				},
				CorrectAnswer = question.CorrectAnswer,
				ScorePoint = question.ScorePoint
			};
		}
		//Add
		public async Task<QuizDTO> AddQuizSetAsync(QuizDTO dto)
		{
			_validator.ValidateAddQuizSet(dto);
			// validate event existence and not started
			var eventDataForAdd = await _uow.Events.GetByIdAsync(dto.EventId);
			if (eventDataForAdd == null)
				throw new Exception("Event không tồn tại");
			if (eventDataForAdd.StartTime <= DateTime.UtcNow)
				throw new Exception("Sự kiện đã bắt đầu, không thể tạo quiz");
			var quiz = new EventQuiz
			{
				EventId = dto.EventId,
				//TopicId = eventDataForAdd.TopicId,
				Title = dto.Title,
				Type = dto.Type,
				PassingScore = dto.PassingScore,
				QuestionSetStatus = QuestionSetEnum.Available,
			};
            

			await _uow.EventQuiz.CreateAsync(quiz);
			await _uow.SaveChangesAsync();
			dto.QuizsetId = quiz.Id;
			return dto;
		}

		public async Task<QuizQuestionDTO> AddQuestionQuizAsync(QuizQuestionDTO dto)
		{
			_validator.ValidateAddQuestion(dto);
			var quiz = await _uow.EventQuiz.GetByIdAsync(dto.QuizId);
			if (quiz == null)
			{
				throw new KeyNotFoundException("Quiz set not found");
			}
			if (quiz.Status == QuizStatusEnum.Published)
			{
				throw new InvalidOperationException("Quiz đã publish, không thể thêm câu hỏi");
			}
			var question = new QuizQuestion
			{
				QuizId = dto.QuizId,
				QuestionText = dto.QuestionText,
				OptionA = GetQuestionOptions(dto).OptionA,
				OptionB = GetQuestionOptions(dto).OptionB,
				OptionC = GetQuestionOptions(dto).OptionC,
				OptionD = GetQuestionOptions(dto).OptionD,
				CorrectAnswer = dto.CorrectAnswer,
				ScorePoint = dto.ScorePoint
			};
			//await _uow.QuizQuestion.CreateAsync(question);
			await _uow.SaveChangesAsync();

			return dto;
		}


		//Upload file quiz
		public async Task<string> UploadFileAsync(string quizId, byte[] fileContent, string fileName)
		{
				if (string.IsNullOrWhiteSpace(fileName))
					throw new ArgumentException("fileName không hợp lệ");
				if (fileContent == null || fileContent.Length == 0)
					throw new ArgumentException("fileContent không hợp lệ");
				var quiz = await _uow.EventQuiz.GetByIdAsync(quizId);
				if (quiz == null)
					throw new KeyNotFoundException("Quiz set not found");
				var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "quiz", quizId);
				Directory.CreateDirectory(uploadsRoot);
			    var safeFileName = Path.GetFileName(fileName);
				var fullPath = Path.Combine(uploadsRoot, safeFileName);
				
			    var ext = Path.GetExtension(fileName).ToLower();
			    var allowExt = new[] { ".pdf", ".txt", ".docx" };
			    if (!allowExt.Contains(ext))
			    {
				    throw new InvalidOperationException("Chỉ cho phép file PDF, TXT hoặc DOCX");
			    }

				await File.WriteAllBytesAsync(fullPath, fileContent);

			    string textContent = "";

			switch (ext)
			{
				case ".txt":
					textContent = await File.ReadAllTextAsync(fullPath);
					break;

				case ".docx":
					textContent = ReadDocx(fullPath);
					break;

				case ".pdf":
					textContent = ReadPdf(fullPath);
					break;
			}

			// convert text -> question object
			var questions = ParseQuestions(textContent);

			// TODO save question vào DB

			var relative = Path.Combine("uploads", "quiz", quizId, safeFileName).Replace('\\', '/');

			quiz.FileQuiz = relative;

			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			return relative;

		}
		//Read Docx
		public string ReadDocx(string path)
		{
			using (var doc = WordprocessingDocument.Open(path, false))
				return doc.MainDocumentPart.Document.Body.InnerText;
		}
		//Read PDF
		public string ReadPdf(string path)
		{
			var text = "";

			using (var pdf = PdfDocument.Open(path))
			{
				foreach (var page in pdf.GetPages())
				{
					text += page.Text;
				}
			}

			return text;
		}
		//Parse to JSON
		public List<QuizQuestionDTO> ParseQuestions(string text)
		{
			var questions = new List<QuizQuestionDTO>();

			var blocks = text.Split("Question:");

			foreach (var block in blocks.Skip(1))
			{
				var lines = block.Split('\n');

				var q = new QuizQuestionDTO
				{
					QuestionText = lines[0].Trim(),
					Option = new List<QuizQuestionOptionDTO>
					{
						new QuizQuestionOptionDTO
						{
							OptionA = lines[1].Trim(),
							OptionB = lines[2].Trim(),
							OptionC = lines[3].Trim(),
							OptionD = lines[4].Trim()
						}
					},
				};

				questions.Add(q);
			}

			return questions;
		}
		//delete
		public async Task<bool> DeleteAsync(QuizDTO dto,string quizId)
        {
            var quiz = await _uow.EventQuiz.GetAsync(q => q.Id == quizId);
            if (quiz == null) throw new KeyNotFoundException("Quiz set not found");
            if (quiz.Status == QuizStatusEnum.Published)
                throw new InvalidOperationException("Quiz đang mở, không thể xóa");
            var eventData = await _uow.Events.GetByIdAsync(quiz.EventId);
            if (eventData != null && eventData.StartTime <= DateTime.UtcNow)
                throw new InvalidOperationException("Sự kiện đã bắt đầu, không thể xóa Quiz");
            await _uow.EventQuiz.RemoveAsync(quiz);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuestionAsync(QuizDTO dto,string questionId)
        {
            /*var question = await _uow.QuizQuestion.GetByIdAsync(questionId);
            if (question == null) throw new KeyNotFoundException("Question not found");
            var quiz = await _uow.EventQuiz.GetByIdAsync(question.QuizId);
            if (quiz != null)
            {
                if (quiz.Status == QuizStatusEnum.Published)
                    throw new InvalidOperationException("Quiz đã publish, không thể chỉnh sửa");
                var eventData = await _uow.Events.GetByIdAsync(quiz.EventId);
                if (eventData != null && eventData.StartTime <= DateTime.UtcNow)
                    throw new InvalidOperationException("Sự kiện đã bắt đầu, không thể xóa câu hỏi");
            }
            //await _uow.QuizQuestion.RemoveAsync(question);
            await _uow.SaveChangesAsync();*/
            return true;
        }

		/*public async Task<List<QuizDTO>> GetAllAsync()
		{
			
				var quizzes = await _uow.EventQuiz.GetAllAsync(null, q => q.Include(x => x.QuizQuestions).Include(x => x.StudentQuizScores));
				return quizzes.Select(q => new QuizDTO
				{
					QuizsetId = q.Id,
					EventId = q.EventId,
					Title = q.Title,
					Type = q.Type,
					PassingScore = q.PassingScore,
					QuestionSetStatus = q.QuestionSetStatus,
					FileQuiz = q.FileQuiz,
					Questions = q.QuizQuestions.Select(MapQuestionToDto).ToList(),
					AttemptCount = q.StudentQuizScores?.Count ?? 0,
					PassedCount = q.StudentQuizScores?.Count(s => s.IsPassed) ?? 0,
					Status = q.Status
				}).ToList();
			
		}*/

		/*public async Task<List<QuizDTO>> GetByEventIdAsync(string eventId)
		{
			
				var quizzes = await _uow.EventQuiz.GetAllAsync(q => q.EventId == eventId, q => q.Include(x => x.QuizQuestions).Include(x => x.StudentQuizScores));
				return quizzes.Select(q => new QuizDTO
				{
					QuizsetId = q.Id,
					EventId = q.EventId,
					Title = q.Title,
					Type = q.Type,
					PassingScore = q.PassingScore,
					QuestionSetStatus = q.QuestionSetStatus,
					FileQuiz = q.FileQuiz,
					Questions = q.QuizQuestions.Select(MapQuestionToDto).ToList(),
					AttemptCount = q.StudentQuizScores?.Count ?? 0,
					PassedCount = q.StudentQuizScores?.Count(s => s.IsPassed) ?? 0,
					Status = q.Status
				}).ToList();
			
		}*/

		/*public async Task<QuizDTO?> GetByIdAsync(string quizId)
		{
			
				var q = await _uow.EventQuiz.GetAsync(x => x.Id == quizId, inc => inc.Include(x => x.QuizQuestions).Include(x => x.StudentQuizScores));
				if (q == null) return null;
				return new QuizDTO
				{
					QuizsetId = q.Id,
					EventId = q.EventId,
					Title = q.Title,
					Type = q.Type,
					PassingScore = q.PassingScore,
					QuestionSetStatus = q.QuestionSetStatus,
					FileQuiz = q.FileQuiz,
					Questions = q.QuizQuestions.Select(MapQuestionToDto).ToList(),
					AttemptCount = q.StudentQuizScores?.Count ?? 0,
					PassedCount = q.StudentQuizScores?.Count(s => s.IsPassed) ?? 0,
					Status = q.Status
				};
			
		}*/

		/*public async Task<List<QuizQuestionDTO>> GetQuestionsAsync(string quizId)
		{
			
				var questions = await _uow.QuizQuestion.GetAllAsync(q => q.QuizId == quizId);
				return questions.Select(MapQuestionToDto).ToList();
			
		}*/

		public async Task<List<StudentQuizScoreDTO>> GetScoresByQuizAsync(string quizId)
		{
			
				var quiz = await _uow.EventQuiz.GetAsync(q => q.Id == quizId, inc => inc.Include(x => x.StudentQuizScores));
				if (quiz == null) return new List<StudentQuizScoreDTO>();
				return quiz.StudentQuizScores.Select(s => new StudentQuizScoreDTO
				{
					Id = s.Id,
					//QuizId = s.QuizId,
					//StudentId = s.StudentId,
					//TotalScore = s.TotalScore,
					//IsPassed = s.IsPassed,
					SubmittedAt = null
				}).ToList();
			
		}

		public async Task<StudentQuizScoreDTO?> GetStudentScoreAsync(string quizId, string studentId)
		{
			
				var quiz = await _uow.EventQuiz.GetAsync(q => q.Id == quizId, inc => inc.Include(x => x.StudentQuizScores));
				if (quiz == null) return null;
				var score = quiz.StudentQuizScores.FirstOrDefault(s => s.StudentId == studentId);
				if (score == null) return null;
				return new StudentQuizScoreDTO
				{
					Id = score.Id,
					//QuizId = score.QuizId,
					//StudentId = score.StudentId,
					//TotalScore = score.TotalScore,
					//IsPassed = score.IsPassed,
					SubmittedAt = null
				};
			
		}
		//submit quiz
		/*public async Task<StudentQuizScoreDTO> SubmitQuizAsync(string quizId, string studentId, Dictionary<string, string> answers)
		{
			if (string.IsNullOrWhiteSpace(quizId))
				throw new ArgumentException("QuizId không hợp lệ");

            if (string.IsNullOrWhiteSpace(studentId))
				throw new ArgumentException("StudentId không hợp lệ");

			if (answers == null || !answers.Any())
				throw new ArgumentException("Answers không hợp lệ");

			/*var quiz = await _uow.EventQuiz.GetAsync(
				q => q.Id == quizId,
				inc => inc.Include(x => x.QuizQuestions)
						  .Include(x => x.StudentQuizScores));

			if (quiz == null)
				throw new KeyNotFoundException("Quiz not found");

			// ensure quiz is published and available
			if (quiz.Status != QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz chưa được publish");

			if (quiz.QuestionSetStatus != QuestionSetEnum.Available)
				throw new InvalidOperationException("Quiz không khả dụng để nộp bài");

			int totalScore = 0;

			foreach (var answer in answers)
			{
				var questionId = answer.Key;
				var selectedAnswer = answer.Value;

				var question = quiz.QuizQuestions.FirstOrDefault(q => q.Id == questionId);
				if (question == null)
					continue;

				if (!string.IsNullOrWhiteSpace(question.CorrectAnswer) &&
					string.Equals(question.CorrectAnswer.Trim(),
								  selectedAnswer?.Trim(),
								  StringComparison.OrdinalIgnoreCase))
				{
					totalScore += question.ScorePoint ?? 0;
				}
			}

			int passingScore = quiz.PassingScore ?? 0;
			bool isPassed = totalScore >= passingScore;

			var existingScore = quiz.StudentQuizScores.FirstOrDefault(s => s.StudentId == studentId);
			if (existingScore != null)
				throw new InvalidOperationException("Bạn đã làm quiz rồi");

			var newScore = new StudentQuizScore
			{
				//QuizId = quizId,
				//StudentId = studentId,
				//TotalScore = totalScore,
				//IsPassed = isPassed
			};

			quiz.StudentQuizScores.Add(newScore);
			existingScore = newScore;

			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			return new StudentQuizScoreDTO
			{
				Id = existingScore.Id,
				//QuizId = quizId,
				//StudentId = studentId,
				//TotalScore = totalScore,
				//IsPassed = isPassed,
				SubmittedAt = DateTime.UtcNow
			};


}*/

		//Update
		public async Task<QuizQuestionDTO> UpdateQuestionAsync(string questionId,QuizQuestionDTO dto,QuizDTO qdto)
		{
			_validator.ValidateUpdateQuizQuestion(dto);
			/*var question = await _uow.QuizQuestion.GetByIdAsync(questionId);
			if (question == null)
				throw new KeyNotFoundException("Question không tồn tại");
			var options = GetQuestionOptions(dto);
			question.QuestionText = dto.QuestionText;
			question.OptionA = options.OptionA;
			question.OptionB = options.OptionB;
	        question.OptionC = options.OptionC;
			question.OptionD = options.OptionD;
			question.CorrectAnswer = dto.CorrectAnswer;	
			question.ScorePoint = dto.ScorePoint;
			await _uow.QuizQuestion.UpdateAsync(question);*/
			await _uow.SaveChangesAsync();
			return dto;
		}
        public async Task<QuizDTO> UpdateQuizSetAsync(string quizsetId,QuizDTO dto)
        {
            _validator.ValidateUpdateQuizSet(dto);

            var quiz = await _uow.EventQuiz.GetByIdAsync(quizsetId);
            if (quiz == null)
                throw new KeyNotFoundException("Quiz không tồn tại");
            if (quiz.Status == QuizStatusEnum.Published)
                throw new InvalidOperationException("Quiz đang mở, không thể chỉnh sửa");
            if (quiz.EventId != dto.EventId)
            {
                throw new InvalidOperationException("Quiz không thuộc event này");
            }
			var eventData = await _uow.Events.GetByIdAsync(dto.EventId);
            if (eventData == null)
				throw new ArgumentException("Event không tồn tại.");
			if (eventData.StartTime <= DateTime.UtcNow)
				throw new Exception("Sự kiện đã bắt đầu, không thể cập nhật Quiz");

			// update quizset
			quiz.Title = dto.Title;
			quiz.Type = dto.Type;
			//quiz.TopicId = eventData.TopicId;
			_validator.ValidatePassingScorewithQuestion(dto);
			quiz.PassingScore = dto.PassingScore;

			// update questions
			_validator.ValidateCheckDuplicateQuestion(quizsetId,dto);
			/*if (dto.Questions != null && dto.Questions.Any())
			{
				//var questions = await _uow.QuizQuestion.GetAllAsync(q => q.QuizId == quizsetId);
				foreach (var q in dto.Questions)
				{
					var options = GetQuestionOptions(q);
					var question = questions.FirstOrDefault(x => x.Id == q.Id);

					if (question == null)
						continue;

					question.QuestionText = q.QuestionText;
					question.OptionA = options.OptionA;
					question.OptionB = options.OptionB;
					question.OptionC = options.OptionC;
					question.OptionD = options.OptionD;
					question.CorrectAnswer = q.CorrectAnswer;
					question.ScorePoint = q.ScorePoint;
				}
			}*/
		   
			await _uow.SaveChangesAsync();

			return dto;
		}

		
	}
}
