using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.ValidationData.Quiz
{
	public class QuizValidator : IQuizValidator
	{
		private readonly IUnitOfWork _uow;
		public QuizValidator(IUnitOfWork uow)
		{
			_uow = uow;
		}

		public void ValidateAddQuizSet(CreateQuizSetRequestDto quizset)
		{
			if (quizset == null)
				throw new ArgumentNullException(nameof(quizset), "Quiz Set không tồn tại.");
			
			if (string.IsNullOrWhiteSpace(quizset.Title))
				throw new ArgumentException("Quiz title không hợp lệ.");

			if (string.IsNullOrWhiteSpace(quizset.EventId))
				throw new ArgumentException("EventId không hợp lệ.");

			if (quizset.Type == QuizTypeEnum.LiveQuiz)
			{
				if (string.IsNullOrWhiteSpace(quizset.LiveQuizLink))
					throw new ArgumentException("Live quiz bắt buộc phải có link.");

				if (!Uri.TryCreate(quizset.LiveQuizLink, UriKind.Absolute, out _))
					throw new ArgumentException("Link live quiz không hợp lệ.");
			}

			if (quizset.TimeLimit.HasValue && quizset.TimeLimit < 0)
				throw new ArgumentException("TimeLimit >= 0");

			if (quizset.QuizStartTime.HasValue && quizset.QuizEndTime.HasValue && quizset.QuizEndTime <= quizset.QuizStartTime)
				throw new ArgumentException("QuizEndTime phải lớn hơn QuizStartTime.");
		}

		public void ValidateAddQuestion(AddQuizQuestionRequestDto question)
		{
			if (question == null)
				throw new ArgumentNullException(nameof(question));

			if (string.IsNullOrWhiteSpace(question.QuizId))
				throw new ArgumentException("QuizId không hợp lệ.");

			if (string.IsNullOrWhiteSpace(question.QuestionText))
				throw new ArgumentException("Question text không hợp lệ.");

			if (question.TypeOption == QuestionTypeOptionEnum.TrueFalse)
			{
				if (string.IsNullOrWhiteSpace(question.CorrectAnswer) || (question.CorrectAnswer != "A" && question.CorrectAnswer != "B"))
					throw new ArgumentException("True/False chỉ chấp nhận đáp án đúng là True hoặc False.");
			}
			else
			{
				if (string.IsNullOrWhiteSpace(question.Options.OptionA)
					|| string.IsNullOrWhiteSpace(question.Options.OptionB)
					|| string.IsNullOrWhiteSpace(question.Options.OptionC)
					|| string.IsNullOrWhiteSpace(question.Options.OptionD))
				{
					throw new ArgumentException("Phải nhập đủ 4 đáp án.");
				}
			}

			if (question.ScorePoint <= 0)
				throw new ArgumentException("ScorePoint phải > 0");

			if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
				throw new ArgumentException("CorrectAnswer không hợp lệ");
		}

		public void ValidateUpdateQuizSet(UpdateQuizSetRequestDto dto)
		{
			if (dto == null)
				throw new ArgumentNullException(nameof(dto), "Quiz Set không tồn tại.");

			if (string.IsNullOrWhiteSpace(dto.QuizId))
				throw new ArgumentException("QuizSetId không hợp lệ.");

			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException("Quiz title không hợp lệ.");

			if (string.IsNullOrWhiteSpace(dto.EventId))
				throw new ArgumentException("EventId không hợp lệ.");

			if (dto.PassingScore < 0)
				throw new ArgumentException("PassingScore >= 0");

			if (dto.TimeLimit.HasValue && dto.TimeLimit < 0)
				throw new ArgumentException("TimeLimit >= 0");

			if (dto.QuizStartTime.HasValue && dto.QuizEndTime.HasValue && dto.QuizEndTime <= dto.QuizStartTime)
				throw new ArgumentException("QuizEndTime phải lớn hơn QuizStartTime.");

			if (!Enum.IsDefined(typeof(QuizTypeEnum), dto.Type))
				throw new ArgumentException("Quiz type không hợp lệ.");

			if (dto.Type == QuizTypeEnum.LiveQuiz)
			{
				if (string.IsNullOrWhiteSpace(dto.LiveQuizLink))
					throw new ArgumentException("Live quiz bắt buộc phải có link.");

				if (!Uri.TryCreate(dto.LiveQuizLink, UriKind.Absolute, out _))
					throw new ArgumentException("Link live quiz không hợp lệ.");
			}
		}

		public void ValidateUpdateQuizQuestion(QuizQuestionContract dto)
		{
			if (dto == null)
				throw new ArgumentNullException(nameof(dto));

			if (string.IsNullOrWhiteSpace(dto.QuestionText))
				throw new ArgumentException("Question text không hợp lệ");

			var options = new[] { dto.Options.OptionA, dto.Options.OptionB, dto.Options.OptionC, dto.Options.OptionD };
			if (options.Count(o => !string.IsNullOrWhiteSpace(o)) < 2)
				throw new ArgumentException("Phải có ít nhất 2 option.");

			if (dto.ScorePoint <= 0)
				throw new ArgumentException("ScorePoint phải > 0");

			if (string.IsNullOrWhiteSpace(dto.CorrectAnswer))
				throw new ArgumentException("Correct answer không hợp lệ");
		}

		public void ValidatePassingScorewithQuestion(int? passingScore, IEnumerable<QuizQuestionContract> questions)
		{
			var questionList = questions?.ToList() ?? new List<QuizQuestionContract>();
			if (!questionList.Any() || !passingScore.HasValue)
				return;

			var totalScore = questionList.Sum(q => q.ScorePoint);

			if (passingScore > totalScore)
				throw new ArgumentException("Passing score không thể lớn hơn tổng điểm");
		}

		public void ValidateCheckDuplicateQuestion(IEnumerable<QuizQuestionContract> questions)
		{
			var duplicate = (questions ?? Enumerable.Empty<QuizQuestionContract>())
			     .GroupBy(q => (q.QuestionText ?? "").Trim().ToLower())
			     .Any(g => g.Count() > 1);
			if (duplicate)
				throw new Exception("Danh sách câu hỏi bị trùng nội dung");
		}

		public void ValidateQuestionCount(IEnumerable<QuizQuestionContract> questions)
		{
			if (questions == null || !questions.Any())
				throw new Exception("Quiz phải có ít nhất 1 câu hỏi");
		}

		public void ValidatePublishQuiz(DataAccess.Entities.EventQuiz quiz)
		{
			if (quiz == null)
				throw new ArgumentNullException(nameof(quiz));

			if (quiz.Event == null)
				throw new InvalidOperationException("Quiz không gắn với sự kiện hợp lệ.");

			// Nếu thời gian hiện tại chưa đến thời gian bắt đầu sự kiện thì không cho publish
			if (quiz.Event.StartTime > DateTime.UtcNow)
			{
				throw new InvalidOperationException($"Sự kiện chưa diễn ra (Bắt đầu lúc: {quiz.Event.StartTime:dd/MM/yyyy HH:mm}). Không thể publish quiz.");
			}
		}
	}
}
