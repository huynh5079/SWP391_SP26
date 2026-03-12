using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.QuizForAll;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz.ForAll
{
    public class QuizSrviceForAll : IQuizServiceForAll
    {
        private readonly IUnitOfWork _uow;

        public QuizSrviceForAll(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<StartQuizResponseDto> StartQuizAsync(StartQuizRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.QuizId))
                throw new ArgumentException("QuizId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(request.StudentId))
                throw new ArgumentException("StudentId không hợp lệ.");

            var quiz = await GetQuizForStudentAsync(request.QuizId);
            if (quiz.Status != QuizStatusEnum.Published)
                throw new InvalidOperationException("Quiz chưa được publish.");
            if (quiz.QuestionSetStatus != QuestionSetEnum.Available || !quiz.EventQuizQuestions.Any())
                throw new InvalidOperationException("Quiz chưa có câu hỏi để làm bài.");

			var session = await _uow.StudentQuizScores.GetAsync(
				x => x.EventQuizId == request.QuizId && x.StudentId == request.StudentId,
				q => q.Include(x => x.StudentAnswers));

			if (session != null)
				throw new InvalidOperationException("Quiz already started");

			session = new StudentQuizScore
			{
				EventQuizId = request.QuizId,
				StudentId = request.StudentId,
				Score = 0,
				StartedAt = DateTime.UtcNow,
				Status = StudentQuizScoreStatusEnum.InProgress
			};

			try
			{
				await _uow.StudentQuizScores.CreateAsync(session);
				await _uow.SaveChangesAsync();
			}
			catch (DbUpdateException)
			{
				throw new InvalidOperationException("Quiz already started");
			}

            var questions = quiz.EventQuizQuestions
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.OrderIndex)
                .Select(MapQuestionContract)
                .ToList();

            return new StartQuizResponseDto
            {
                Session = new StudentQuizSessionDto
                {
                    StudentQuizScoreId = session.Id,
                    Quiz = MapQuizSummaryContract(quiz, questions.Count),
                    Questions = questions,
                    Limit = BuildTimeLimit(quiz, session.StartedAt)
                }
            };
        }

		public async Task<GetCurrentQuizSessionResponseDto> GetCurrentQuizSessionAsync(GetCurrentQuizSessionRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(request.QuizId))
				throw new ArgumentException("QuizId không hợp lệ.");
			if (string.IsNullOrWhiteSpace(request.StudentId))
				throw new ArgumentException("StudentId không hợp lệ.");

			var quiz = await GetQuizForStudentAsync(request.QuizId);
			var session = await _uow.StudentQuizScores.GetAsync(
				x => x.EventQuizId == request.QuizId && x.StudentId == request.StudentId && x.Status == StudentQuizScoreStatusEnum.InProgress,
				q => q.Include(x => x.StudentAnswers));

			if (session == null)
			{
				return new GetCurrentQuizSessionResponseDto();
			}

			var questions = quiz.EventQuizQuestions
				.Where(x => x.DeletedAt == null)
				.OrderBy(x => x.OrderIndex)
				.Select(MapQuestionContract)
				.ToList();

			return new GetCurrentQuizSessionResponseDto
			{
				Session = new StudentQuizSessionDto
				{
					StudentQuizScoreId = session.Id,
					Quiz = MapQuizSummaryContract(quiz, questions.Count),
					Questions = questions,
					Limit = BuildTimeLimit(quiz, session.StartedAt)
				}
			};
		}

        public async Task<SubmitQuizResponseDto> SubmitQuizAsync(SubmitQuizRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.QuizId))
                throw new ArgumentException("QuizId không hợp lệ.");
            if (string.IsNullOrWhiteSpace(request.StudentId))
                throw new ArgumentException("StudentId không hợp lệ.");

            var quiz = await GetQuizForStudentAsync(request.QuizId);
            var session = await GetStudentSessionAsync(request);
            if (session == null)
                throw new InvalidOperationException("Không tìm thấy phiên làm quiz.");
            if (session.Status == StudentQuizScoreStatusEnum.Submitted)
                throw new InvalidOperationException("Quiz đã được nộp.");

            var submittedAt = DateTime.UtcNow;
            var isTimedOut = quiz.TimeLimit.HasValue
                && quiz.TimeLimit.Value > 0
                && session.StartedAt.HasValue
                && submittedAt > session.StartedAt.Value.AddMinutes(quiz.TimeLimit.Value);

			if (isTimedOut)
				throw new InvalidOperationException("Quiz time expired");

			using var transaction = await _uow.BeginTransactionAsync();
			try
			{

				var existingAnswers = (await _uow.StudentAnswers.GetAllAsync(x => x.StudentQuizScoreId == session.Id && x.DeletedAt == null)).ToList();
				foreach (var existingAnswer in existingAnswers)
				{
					await _uow.StudentAnswers.RemoveAsync(existingAnswer);
				}

				var submittedAnswers = request.Answers ?? new List<SubmitQuizAnswerDto>();
				var resultAnswers = new List<StudentAnswerContract>();
				var totalScore = 0;

				foreach (var question in quiz.EventQuizQuestions.Where(x => x.DeletedAt == null && !string.IsNullOrWhiteSpace(x.QuestionBankId)).OrderBy(x => x.OrderIndex))
				{
					var submittedAnswer = submittedAnswers.FirstOrDefault(x => x.QuestionBankId == question.QuestionBankId);
					var questionType = DetermineQuestionType(question.OptionA, question.OptionB, question.OptionC, question.OptionD, question.CorrectAnswer);
					var normalizedSelectedAnswer = NormalizeAnswers(submittedAnswer?.SelectedAnswer, questionType);
					var normalizedCorrectAnswer = NormalizeAnswers(question.CorrectAnswer, questionType);
					var isCorrect = !string.IsNullOrWhiteSpace(normalizedSelectedAnswer)
						&& string.Equals(normalizedSelectedAnswer, normalizedCorrectAnswer, StringComparison.OrdinalIgnoreCase);
					var earnedScore = isCorrect ? question.ScorePoint : 0;

					var answerEntity = new StudentAnswer
					{
						StudentQuizScoreId = session.Id,
						QuestionBankId = question.QuestionBankId!,
						SelectedAnswer = normalizedSelectedAnswer,
						IsCorrect = isCorrect,
						ScoreEarned = earnedScore
					};

					await _uow.StudentAnswers.CreateAsync(answerEntity);

					resultAnswers.Add(new StudentAnswerContract
					{
						StudentAnswerId = answerEntity.Id,
						StudentQuizScoreId = session.Id,
						QuestionBankId = question.QuestionBankId!,
						SelectedAnswer = normalizedSelectedAnswer,
						IsCorrect = isCorrect,
						ScoreEarned = earnedScore
					});

					totalScore += earnedScore;
				}

				session.Score = totalScore;
				session.SubmittedAt = submittedAt;
				session.Status = StudentQuizScoreStatusEnum.Submitted;
				await _uow.StudentQuizScores.UpdateAsync(session);
				await _uow.SaveChangesAsync();
				await transaction.CommitAsync();

				return new SubmitQuizResponseDto
				{
					QuizId = request.QuizId,
					StudentQuizScoreId = session.Id,
					Score = totalScore,
					IsPassed = totalScore >= (quiz.PassingScore ?? 0),
					AllowReview = quiz.AllowReview,
					IsTimedOut = false,
					SubmittedAt = submittedAt,
					Answers = resultAnswers,
					ReviewQuestions = quiz.AllowReview
						? quiz.EventQuizQuestions
							.Where(x => x.DeletedAt == null)
							.OrderBy(x => x.OrderIndex)
							.Select(MapReviewQuestionContract)
							.ToList()
						: new List<QuizQuestionContract>()
				};
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
        }

        private async Task<EventQuiz> GetQuizForStudentAsync(string quizId)
        {
            var quiz = await _uow.EventQuiz.GetAsync(
                x => x.Id == quizId,
                q => q.Include(x => x.EventQuizQuestions)
                    .Include(x => x.QuizSet)
                    .Include(x => x.StudentQuizScores));

            if (quiz == null)
                throw new KeyNotFoundException("Quiz không tồn tại.");

            return quiz;
        }

        private async Task<StudentQuizScore?> GetStudentSessionAsync(SubmitQuizRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.StudentQuizScoreId))
            {
                return await _uow.StudentQuizScores.GetAsync(
                    x => x.Id == request.StudentQuizScoreId && x.StudentId == request.StudentId && x.EventQuizId == request.QuizId,
                    q => q.Include(x => x.StudentAnswers));
            }

            return await _uow.StudentQuizScores.GetAsync(
                x => x.EventQuizId == request.QuizId && x.StudentId == request.StudentId && x.Status == StudentQuizScoreStatusEnum.InProgress,
                q => q.Include(x => x.StudentAnswers));
        }

        private static QuizTimeLimitDto BuildTimeLimit(EventQuiz quiz, DateTime? startedAt)
        {
            DateTime? endsAt = quiz.TimeLimit.HasValue && quiz.TimeLimit.Value > 0 && startedAt.HasValue
                ? startedAt.Value.AddMinutes(quiz.TimeLimit.Value)
                : null;

            return new QuizTimeLimitDto
            {
                TimeLimitMinutes = quiz.TimeLimit,
                StartedAt = startedAt,
                EndsAt = endsAt,
                IsTimedOut = endsAt.HasValue && DateTime.UtcNow > endsAt.Value
            };
        }

        private static QuizSummaryContract MapQuizSummaryContract(EventQuiz quiz, int questionCount)
        {
            return new QuizSummaryContract
            {
                EventQuizId = quiz.Id,
                QuizSetId = quiz.QuizSetId ?? string.Empty,
                EventId = quiz.EventId,
                Title = quiz.Title,
                FileQuiz = quiz.FileQuiz,
                LiveQuizLink = quiz.LiveQuizLink,
                Type = quiz.Type,
                Status = quiz.Status,
                QuestionSetStatus = quiz.QuestionSetStatus,
                PassingScore = quiz.PassingScore,
                TimeLimit = quiz.TimeLimit,
				AllowReview = quiz.AllowReview,
                IsActive = quiz.IsActive,
                QuestionCount = questionCount,
                AttemptCount = quiz.StudentQuizScores?.Count ?? 0,
                PassedCount = quiz.StudentQuizScores?.Count(x => (x.Score ?? 0) >= (quiz.PassingScore ?? 0)) ?? 0,
                CreatedAt = quiz.CreatedAt,
                UpdatedAt = quiz.UpdatedAt
            };
        }

        private static QuizQuestionContract MapQuestionContract(EventQuizQuestion question)
        {
            return new QuizQuestionContract
            {
                QuestionBankId = question.QuestionBankId ?? string.Empty,
                EventQuizQuestionId = question.Id,
                QuizSetQuestionId = question.Id,
                QuestionText = question.QuestionText,
                Options = new QuizQuestionOptionContract
                {
                    OptionA = question.OptionA,
                    OptionB = question.OptionB,
                    OptionC = question.OptionC,
                    OptionD = question.OptionD
                },
                TypeOption = DetermineQuestionType(question.OptionA, question.OptionB, question.OptionC, question.OptionD, question.CorrectAnswer),
				CorrectAnswer = null,
				Explanation = null,
                ScorePoint = question.ScorePoint,
                OrderIndex = question.OrderIndex,
                Difficulty = question.Difficulty
            };
        }

		private static QuizQuestionContract MapReviewQuestionContract(EventQuizQuestion question)
		{
			return new QuizQuestionContract
			{
				QuestionBankId = question.QuestionBankId ?? string.Empty,
				EventQuizQuestionId = question.Id,
				QuizSetQuestionId = question.Id,
				QuestionText = question.QuestionText,
				Options = new QuizQuestionOptionContract
				{
					OptionA = question.OptionA,
					OptionB = question.OptionB,
					OptionC = question.OptionC,
					OptionD = question.OptionD
				},
				TypeOption = DetermineQuestionType(question.OptionA, question.OptionB, question.OptionC, question.OptionD, question.CorrectAnswer),
				CorrectAnswer = question.CorrectAnswer,
				Explanation = question.Explanation,
				ScorePoint = question.ScorePoint,
				OrderIndex = question.OrderIndex,
				Difficulty = question.Difficulty
			};
		}

        private static QuestionTypeOptionEnum DetermineQuestionType(string optionA, string optionB, string? optionC, string? optionD, string? correctAnswer)
        {
            if (string.Equals(optionA, "True", StringComparison.OrdinalIgnoreCase)
                && string.Equals(optionB, "False", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(optionC)
                && string.IsNullOrWhiteSpace(optionD))
            {
                return QuestionTypeOptionEnum.TrueFalse;
            }

            if (!string.IsNullOrWhiteSpace(correctAnswer) && correctAnswer.Contains(','))
            {
                return QuestionTypeOptionEnum.MultipleChoice;
            }

            return QuestionTypeOptionEnum.SingleChoice;
        }

        private static string? NormalizeAnswers(string? value, QuestionTypeOptionEnum typeOption)
        {
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			var normalized = value.Replace(" ", string.Empty).ToUpperInvariant();
			if (!Regex.IsMatch(normalized, @"^[A-D](,[A-D])*$"))
			{
				throw new ArgumentException("Invalid answer format");
			}

			var answers = normalized
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Distinct()
				.OrderBy(x => x)
				.ToList();

			if (typeOption == QuestionTypeOptionEnum.MultipleChoice)
			{
				return string.Join(",", answers);
			}

			if (answers.Count != 1)
			{
				throw new ArgumentException("Invalid answer format");
			}

			if (typeOption == QuestionTypeOptionEnum.TrueFalse && answers[0] != "A" && answers[0] != "B")
			{
				throw new ArgumentException("Invalid answer format");
			}

			return answers[0];
        }
    }
}
