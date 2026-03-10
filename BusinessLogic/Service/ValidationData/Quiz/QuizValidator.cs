using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz;
using BusinessLogic.DTOs.Event.Quiz;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BusinessLogic.Service.ValidationData.Quiz
{
	public class QuizValidator : IQuizValidator
	{
		private readonly IUnitOfWork _uow;
		public QuizValidator(IUnitOfWork uow)
		{
			_uow = uow;
		}
		//add
		public void ValidateAddQuizSet(QuizDTO quizset)
		{
			if (quizset == null)
				throw new ArgumentNullException(nameof(quizset), "Quiz Set không tồn tại.");
			
			if (string.IsNullOrWhiteSpace(quizset.Title))
				throw new ArgumentException("Quiz title không hợp lệ.");

			if (string.IsNullOrWhiteSpace(quizset.EventId))
				throw new ArgumentException("EventId không hợp lệ.");
		}
		public void ValidateAddQuestion(QuizQuestionDTO question)
		{
			if (question == null)
				throw new ArgumentNullException(nameof(question));

			if (string.IsNullOrWhiteSpace(question.QuizId))
				throw new ArgumentException("QuizId không hợp lệ.");

			if (string.IsNullOrWhiteSpace(question.QuestionText))
				throw new ArgumentException("Question text không hợp lệ.");

            // ensure at least two options
            if (string.IsNullOrWhiteSpace(question.OptionA) || string.IsNullOrWhiteSpace(question.OptionB))
            {
                throw new ArgumentException("Phải có ít nhất 2 option");
            }

            if (question.ScorePoint <= 0)
                throw new ArgumentException("ScorePoint phải > 0");

            if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
                throw new ArgumentException("CorrectAnswer không hợp lệ");
		}
		//update
		public void ValidateUpdateQuizSet(QuizDTO dto)
		{
			if (dto == null)
				throw new ArgumentNullException(nameof(dto), "Quiz Set không tồn tại.");

			if (string.IsNullOrWhiteSpace(dto.QuizsetId))
				throw new ArgumentException("QuizSetId không hợp lệ.");

			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException("Quiz title không hợp lệ.");

			if (string.IsNullOrWhiteSpace(dto.EventId))
				throw new ArgumentException("EventId không hợp lệ.");

			if (dto.PassingScore < 0)
				throw new ArgumentException("PassingScore >= 0");

			if (!Enum.IsDefined(typeof(QuizTypeEnum), dto.Type))
				throw new ArgumentException("Quiz type không hợp lệ.");
			
			
		}

	public void ValidateUpdateQuizQuestion(QuizQuestionDTO dto)
	{
		if (dto == null)
			throw new ArgumentNullException(nameof(dto));

		// do not validate QuizId here - service already fetched the question by id
		if (string.IsNullOrWhiteSpace(dto.QuestionText))
			throw new ArgumentException("Question text không hợp lệ");

		var options = new[] { dto.OptionA, dto.OptionB, dto.OptionC, dto.OptionD };

		var validOptions = options.Count(o => !string.IsNullOrWhiteSpace(o));

		if (validOptions < 2)
			throw new ArgumentException("Phải có ít nhất 2 option.");

		if (dto.ScorePoint <= 0)
			throw new ArgumentException("ScorePoint phải > 0");

		if (string.IsNullOrWhiteSpace(dto.CorrectAnswer))
			throw new ArgumentException("Correct answer không hợp lệ");
	}



		public void ValidatePassingScorewithQuestion(QuizDTO dto)
		{
			if (dto.Questions == null || !dto.Questions.Any())
				return;

			var totalScore = dto.Questions.Sum(q => q.ScorePoint ?? 0);

			if (dto.PassingScore > totalScore)
				throw new ArgumentException("Passing score không thể lớn hơn tổng điểm");
		}

		public void ValidateCheckDuplicateQuestion(string quizsetId, QuizDTO dto)
		{
			var duplicate = dto.Questions
			     .GroupBy(q => (q.QuestionText ?? "").Trim().ToLower())
			     .Any(g => g.Count() > 1);
			if (duplicate)
				throw new Exception("Danh sách câu hỏi bị trùng nội dung");
		}

		public void ValidateQuestionCount(string quizsetId, QuizDTO dto)
		{
			if (dto.Questions == null || !dto.Questions.Any())
				throw new Exception("Quiz phải có ít nhất 1 câu hỏi");
		}

		

		
	}
}
