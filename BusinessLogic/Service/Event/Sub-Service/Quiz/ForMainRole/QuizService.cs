using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.AddQuestion;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuizScores;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.QuizActions;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UpdateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.UploadQuizFile;
using BusinessLogic.Service.ValidationData.Quiz;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace BusinessLogic.Service.Event.Sub_Service.Quiz
{
	public class QuizService : IQuizService
	{
		private readonly IUnitOfWork _uow;
		private readonly IQuizValidator _validator;

		public QuizService(IUnitOfWork uow, IQuizValidator validator)
		{
			_uow = uow;
			_validator = validator;
		}

		private static QuizQuestionOptionContract GetQuestionOptions(QuizQuestionContract dto)
		{
			return dto.Options ?? new QuizQuestionOptionContract();
		}

		private static QuizQuestionOptionContract MapOptions(QuestionBank question)
		{
			return new QuizQuestionOptionContract
			{
				OptionA = question.OptionA,
				OptionB = question.OptionB,
				OptionC = question.OptionC,
				OptionD = question.OptionD
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
				CorrectAnswer = question.CorrectAnswer,
				Explanation = question.Explanation,
				ScorePoint = question.ScorePoint,
				OrderIndex = question.OrderIndex,
				Difficulty = question.Difficulty
			};
		}

		private static QuizQuestionContract MapQuestionContract(QuizSetQuestion quizSetQuestion)
		{
			var question = quizSetQuestion.QuestionBank;
			return new QuizQuestionContract
			{
				QuestionBankId = question?.Id ?? string.Empty,
				EventQuizQuestionId = string.Empty,
				QuizSetQuestionId = quizSetQuestion.Id,
				QuestionText = question?.QuestionText ?? string.Empty,
				Options = question == null ? new QuizQuestionOptionContract() : MapOptions(question),
				TypeOption = question == null
					? QuestionTypeOptionEnum.SingleChoice
					: DetermineQuestionType(question.OptionA, question.OptionB, question.OptionC, question.OptionD, question.CorrectAnswer),
				CorrectAnswer = question?.CorrectAnswer,
				Explanation = question?.Explanation,
				ScorePoint = quizSetQuestion.ScorePoint ?? 1,
				OrderIndex = quizSetQuestion.OrderIndex,
				Difficulty = question?.Difficulty ?? QuestionDifficultyEnum.Medium
			};
		}

		private static QuizSummaryContract MapQuizSummaryContract(EventQuiz quiz, QuizSet? quizSet, int questionCount, int attemptCount, int passedCount)
		{
			return new QuizSummaryContract
			{
				EventQuizId = quiz.Id,
				QuizSetId = quiz.QuizSetId ?? quizSet?.Id ?? string.Empty,
				EventId = quiz.EventId,
				EventTitle = quiz.Event?.Title ?? string.Empty,
				SemesterName = quiz.Event?.Semester?.Name ?? quiz.Event?.Semester?.Code ?? string.Empty,
				SharingStatus = quizSet?.SharingStatus ?? QuizSetVisibilityEnum.Private,
				TopicId = quizSet?.TopicId,
				OrganizerId = quizSet?.OrganizerId,
				Title = quiz.Title,
				Description = quizSet?.Description,
				FileQuiz = quiz.FileQuiz ?? quizSet?.FileQuiz,
				LiveQuizLink = quiz.LiveQuizLink,
				Type = quiz.Type,
				Status = quiz.Status,
				QuestionSetStatus = quiz.QuestionSetStatus,
				PassingScore = quiz.PassingScore,
				TimeLimit = quiz.TimeLimit,
				AllowReview = quiz.AllowReview,
				IsActive = quiz.IsActive,
				QuestionCount = questionCount,
				AttemptCount = attemptCount,
				PassedCount = passedCount,
				CreatedAt = quiz.CreatedAt,
				UpdatedAt = quiz.UpdatedAt
			};
		}

		private static QuizScoreContract MapQuizScoreContract(StudentQuizScore score)
		{
			return new QuizScoreContract
			{
				StudentQuizScoreId = score.Id,
				EventQuizId = score.EventQuizId ?? string.Empty,
				StudentId = score.StudentId,
				Score = score.Score ?? 0,
				StartedAt = score.StartedAt,
				SubmittedAt = score.SubmittedAt,
				Status = score.Status
			};
		}

		private static QuizBankSummaryContract MapQuizBankSummaryContract(QuizSet quizSet, string currentOrganizerId)
		{
			var sourceType = quizSet.OrganizerId == currentOrganizerId
				? QuizBankSourceTypeEnum.Organizer
				: QuizBankSourceTypeEnum.Community;

			return new QuizBankSummaryContract
			{
				QuizSetId = quizSet.Id,
				Title = quizSet.Title,
				TopicId = quizSet.TopicId,
				TopicName = quizSet.Topic?.Name ?? string.Empty,
				OrganizerId = quizSet.OrganizerId,
				OrganizerName = quizSet.Organizer?.User?.FullName ?? string.Empty,
				Description = quizSet.Description,
				FileQuiz = quizSet.FileQuiz,
				SharingStatus = quizSet.SharingStatus,
				SourceType = sourceType,
				QuestionCount = quizSet.QuizSetQuestions.Count(x => x.DeletedAt == null),
				UpdatedAt = quizSet.UpdatedAt
			};
		}

		private static QuestionBank CloneQuestionBank(QuestionBank questionBank, string organizerId, string? topicId)
		{
			return new QuestionBank
			{
				TopicId = topicId ?? questionBank.TopicId,
				OrganizerId = organizerId,
				QuestionText = questionBank.QuestionText,
				OptionA = questionBank.OptionA,
				OptionB = questionBank.OptionB,
				OptionC = questionBank.OptionC,
				OptionD = questionBank.OptionD,
				CorrectAnswer = questionBank.CorrectAnswer,
				Explanation = questionBank.Explanation,
				Difficulty = questionBank.Difficulty
			};
		}

		private async Task<StaffProfile> GetOrganizerAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("UserId khÃ´ng há»£p lá»‡.");
			}

			var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
			if (staff == null)
			{
				throw new InvalidOperationException("ChÆ°a thiáº¿t láº­p há»“ sÆ¡ nhÃ¢n viÃªn (StaffProfile).");
			}

			return staff;
		}

		private static bool TryReadOption(string line, char option, out string value)
		{
			var match = Regex.Match(line.Trim(), $"^{option}\\s*[\\.:\\)\\-]\\s*(.+)$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				value = match.Groups[1].Value.Trim();
				return !string.IsNullOrWhiteSpace(value);
			}

			value = string.Empty;
			return false;
		}

		private static string? NormalizeAnswer(string? value)
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

			return string.Join(",", normalized
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Distinct()
				.OrderBy(x => x));
		}

		private static string? NormalizeAnswers(string? value, QuestionTypeOptionEnum typeOption)
		{
			var normalized = NormalizeAnswer(value);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return null;
			}

			var answers = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (typeOption == QuestionTypeOptionEnum.TrueFalse)
			{
				if (answers.Length != 1 || (answers[0] != "A" && answers[0] != "B"))
				{
					throw new ArgumentException("Invalid answer format");
				}

				return answers[0];
			}

			if (typeOption == QuestionTypeOptionEnum.MultipleChoice)
			{
				return string.Join(",", answers.OrderBy(x => x));
			}

			if (answers.Length != 1)
			{
				throw new ArgumentException("Invalid answer format");
			}

			return answers[0];
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

		private static AddQuizQuestionRequestDto NormalizeQuestionRequest(AddQuizQuestionRequestDto request)
		{
			request.CorrectAnswer = NormalizeAnswers(request.CorrectAnswer, request.TypeOption) ?? string.Empty;

			if (request.TypeOption == QuestionTypeOptionEnum.TrueFalse)
			{
				request.Options.OptionA = "True";
				request.Options.OptionB = "False";
				request.Options.OptionC = null;
				request.Options.OptionD = null;
			}

			return request;
		}

		private async Task<QuizSet?> GetOrganizerQuestionBankAsync(string organizerId, string title)
		{
			return await _uow.QuizSets.GetAsync(x => x.OrganizerId == organizerId && x.Title == title && x.DeletedAt == null);
		}

		private async Task<(QuizSet QuizSet, int QuestionCount)> CreateQuizSetFromSourceAsync(QuizSet sourceQuizSet, StaffProfile organizer, CreateQuizSetRequestDto request, string? fallbackTopicId)
		{
			var quizSet = new QuizSet
			{
				TopicId = request.TopicId ?? sourceQuizSet.TopicId ?? fallbackTopicId,
				OrganizerId = organizer.Id,
				Title = request.Title,
				Description = sourceQuizSet.Description,
				FileQuiz = sourceQuizSet.FileQuiz,
				SharingStatus = request.SharingStatus,
				IsActive = true
			};

			await _uow.QuizSets.CreateAsync(quizSet);

			var sourceQuestions = sourceQuizSet.QuizSetQuestions
				.Where(x => x.DeletedAt == null && x.QuestionBank != null)
				.OrderBy(x => x.OrderIndex)
				.ToList();

			var orderIndex = 1;
			foreach (var sourceQuestion in sourceQuestions)
			{
				var clonedQuestionBank = CloneQuestionBank(sourceQuestion.QuestionBank!, organizer.Id, quizSet.TopicId);
				await _uow.QuestionBanks.CreateAsync(clonedQuestionBank);

				await _uow.QuizSetQuestions.CreateAsync(new QuizSetQuestion
				{
					QuizSetId = quizSet.Id,
					QuestionBankId = clonedQuestionBank.Id,
					ScorePoint = sourceQuestion.ScorePoint ?? 1,
					OrderIndex = orderIndex++
				});
			}

			return (quizSet, sourceQuestions.Count);
		}

		private async Task<EventQuiz> GetOwnedQuizAsync(string quizId, string userId, Func<IQueryable<EventQuiz>, IQueryable<EventQuiz>>? includes = null)
		{
			if (string.IsNullOrWhiteSpace(userId))
				throw new InvalidOperationException("Không xác định được organizer hiện tại.");

			var organizer = await GetOrganizerAsync(userId);
			var quiz = await _uow.EventQuiz.GetAsync(
				x => x.Id == quizId,
				q =>
				{
					var query = q.Include(x => x.Event);
					return includes != null ? includes(query) : query;
				});

			if (quiz == null)
				throw new KeyNotFoundException("Quiz không tồn tại.");

			if (quiz.Event?.OrganizerId != organizer.Id)
				throw new InvalidOperationException("Bạn không có quyền thao tác quiz này.");

			return quiz;
		}

		private static EventQuizQuestion CreateSnapshotQuestion(EventQuiz quiz, QuestionBank questionBank, int orderIndex, int scorePoint)
		{
			return new EventQuizQuestion
			{
				EventQuizId = quiz.Id,
				QuestionBankId = questionBank.Id,
				QuestionText = questionBank.QuestionText,
				OptionA = questionBank.OptionA,
				OptionB = questionBank.OptionB,
				OptionC = questionBank.OptionC,
				OptionD = questionBank.OptionD,
				CorrectAnswer = questionBank.CorrectAnswer,
				Explanation = questionBank.Explanation,
				Difficulty = questionBank.Difficulty,
				ScorePoint = scorePoint,
				OrderIndex = orderIndex
			};
		}

		private async Task RefreshEventQuizQuestionsAsync(EventQuiz quiz)
		{
			var existingSnapshots = (await _uow.EventQuizQuestions.GetAllAsync(x => x.EventQuizId == quiz.Id && x.DeletedAt == null)).ToList();
			foreach (var snapshot in existingSnapshots)
			{
				await _uow.EventQuizQuestions.RemoveAsync(snapshot);
			}

			if (string.IsNullOrWhiteSpace(quiz.QuizSetId))
			{
				quiz.QuestionSetStatus = QuestionSetEnum.NA;
				return;
			}

			var quizSetQuestions = (await _uow.QuizSetQuestions.GetAllAsync(
				x => x.QuizSetId == quiz.QuizSetId && x.DeletedAt == null,
				q => q.Include(x => x.QuestionBank)))
				.OrderBy(x => x.OrderIndex)
				.ToList();

			foreach (var quizSetQuestion in quizSetQuestions.Where(x => x.QuestionBank != null))
			{
				await _uow.EventQuizQuestions.CreateAsync(CreateSnapshotQuestion(
					quiz,
					quizSetQuestion.QuestionBank!,
					quizSetQuestion.OrderIndex,
					quizSetQuestion.ScorePoint ?? 1));
			}

			quiz.QuestionSetStatus = quizSetQuestions.Count > 0 ? QuestionSetEnum.Available : QuestionSetEnum.NA;
		}

		private async Task SyncQuestionBankAsync(QuizSet quizSet, QuestionSetEnum questionSetStatus)
		{
			var linkedQuizzes = (await _uow.EventQuiz.GetAllAsync(x => x.QuizSetId == quizSet.Id && x.DeletedAt == null)).ToList();
			foreach (var linkedQuiz in linkedQuizzes)
			{
				linkedQuiz.FileQuiz = quizSet.FileQuiz;
				linkedQuiz.QuestionSetStatus = questionSetStatus;
				await _uow.EventQuiz.UpdateAsync(linkedQuiz);
			}
		}

		private async Task PersistQuestionsAsync(QuizSet quizSet, StaffProfile organizer, IEnumerable<QuizQuestionContract> questions, bool replaceExisting)
		{
			if (replaceExisting)
			{
				var existingLinks = (await _uow.QuizSetQuestions.GetAllAsync(x => x.QuizSetId == quizSet.Id && x.DeletedAt == null)).ToList();
				foreach (var link in existingLinks)
				{
					await _uow.QuizSetQuestions.RemoveAsync(link);
				}
			}

			var orderIndex = 1;
			foreach (var questionDto in questions.Where(x => !string.IsNullOrWhiteSpace(x.QuestionText)))
			{
				var options = GetQuestionOptions(questionDto);
				var answer = NormalizeAnswers(questionDto.CorrectAnswer, questionDto.TypeOption);
				if (string.IsNullOrWhiteSpace(options.OptionA) || string.IsNullOrWhiteSpace(options.OptionB) || string.IsNullOrWhiteSpace(answer))
				{
					continue;
				}

				var normalizedQuestionText = questionDto.QuestionText.Trim();
				var questionBank = await _uow.QuestionBanks.GetAsync(
					x => x.QuestionText.ToLower() == normalizedQuestionText.ToLower() && x.DeletedAt == null);

				if (questionBank == null)
				{
					questionBank = new QuestionBank
					{
						TopicId = quizSet.TopicId,
						OrganizerId = organizer.Id,
						QuestionText = normalizedQuestionText,
						OptionA = options.OptionA.Trim(),
						OptionB = options.OptionB.Trim(),
						OptionC = string.IsNullOrWhiteSpace(options.OptionC) ? null : options.OptionC.Trim(),
						OptionD = string.IsNullOrWhiteSpace(options.OptionD) ? null : options.OptionD.Trim(),
						CorrectAnswer = answer,
						Explanation = string.IsNullOrWhiteSpace(questionDto.Explanation) ? null : questionDto.Explanation.Trim(),
						Difficulty = questionDto.Difficulty
					};

					await _uow.QuestionBanks.CreateAsync(questionBank);
				}

				var existingQuizSetQuestion = await _uow.QuizSetQuestions.GetAsync(
					x => x.QuizSetId == quizSet.Id && x.QuestionBankId == questionBank.Id && x.DeletedAt == null);
				if (existingQuizSetQuestion != null)
				{
					continue;
				}

				await _uow.QuizSetQuestions.CreateAsync(new QuizSetQuestion
				{
					QuizSetId = quizSet.Id,
					QuestionBankId = questionBank.Id,
					ScorePoint = questionDto.ScorePoint > 0 ? questionDto.ScorePoint : 1,
					OrderIndex = orderIndex++
				});
			}

			var questionSetStatus = orderIndex > 1 ? QuestionSetEnum.Available : QuestionSetEnum.NA;
			await _uow.QuizSets.UpdateAsync(quizSet);
			await SyncQuestionBankAsync(quizSet, questionSetStatus);
		}

		public async Task<GetAvailableQuizBanksResponseDto> GetAvailableQuizBanksAsync(GetAvailableQuizBanksRequestDto request)
		{
			var organizer = await GetOrganizerAsync(request.UserId);

			var quizSets = (await _uow.QuizSets.GetAllAsync(
				x => x.DeletedAt == null
					&& x.IsActive
					&& (x.OrganizerId == organizer.Id || x.SharingStatus == QuizSetVisibilityEnum.Public),
				q => q.Include(x => x.Topic)
					.Include(x => x.Organizer)
						.ThenInclude(x => x!.User)
					.Include(x => x.QuizSetQuestions)))
				.OrderByDescending(x => x.UpdatedAt)
				.ToList();

			return new GetAvailableQuizBanksResponseDto
			{
				QuizBanks = quizSets
					.Where(x => x.OrganizerId == organizer.Id || x.SharingStatus == QuizSetVisibilityEnum.Public)
					.Select(x => MapQuizBankSummaryContract(x, organizer.Id))
					.ToList()
			};
		}

		public async Task<CreateQuizSetResponseDto> CreateQuizSetAsync(CreateQuizSetRequestDto request)
		{
			_validator.ValidateAddQuizSet(request);
			var organizer = await GetOrganizerAsync(request.UserId);
			var eventDataForAdd = await _uow.Events.GetByIdAsync(request.EventId);
			if (eventDataForAdd == null)
				throw new Exception("Event khÃ´ng tá»“n táº¡i");
			if (eventDataForAdd.OrganizerId != organizer.Id)
				throw new InvalidOperationException("Báº¡n khÃ´ng cÃ³ quyá»n táº¡o quiz cho event nÃ y.");
			if (eventDataForAdd.StartTime <= DateTime.UtcNow)
				throw new Exception("Sá»± kiá»‡n Ä‘Ã£ báº¯t Ä‘áº§u, khÃ´ng thá»ƒ táº¡o quiz");

			using var transaction = await _uow.BeginTransactionAsync();
			try
			{
				QuizSet quizSet;
				int questionCount;

				if (!string.IsNullOrWhiteSpace(request.SourceQuizSetId))
				{
					var sourceQuizSet = await _uow.QuizSets.GetAsync(
						x => x.Id == request.SourceQuizSetId && x.DeletedAt == null && x.IsActive,
						q => q.Include(x => x.QuizSetQuestions)
							.ThenInclude(x => x.QuestionBank));

					if (sourceQuizSet == null)
						throw new KeyNotFoundException("Question bank không tồn tại.");

					if (sourceQuizSet.OrganizerId != organizer.Id && sourceQuizSet.SharingStatus != QuizSetVisibilityEnum.Public)
						throw new InvalidOperationException("Bạn không có quyền sử dụng question bank này.");

					(quizSet, questionCount) = await CreateQuizSetFromSourceAsync(sourceQuizSet, organizer, request, eventDataForAdd.TopicId);
				}
				else
				{
					quizSet = await GetOrganizerQuestionBankAsync(organizer.Id, request.Title);
					if (quizSet == null)
					{
						quizSet = new QuizSet
						{
							TopicId = request.TopicId ?? eventDataForAdd.TopicId,
							OrganizerId = organizer.Id,
							Title = request.Title,
							FileQuiz = request.FileQuiz,
							SharingStatus = request.SharingStatus,
							IsActive = true
						};

						await _uow.QuizSets.CreateAsync(quizSet);
					}
					else
					{
						quizSet.TopicId ??= request.TopicId ?? eventDataForAdd.TopicId;
						if (string.IsNullOrWhiteSpace(quizSet.Title))
						{
							quizSet.Title = request.Title;
						}
						if (string.IsNullOrWhiteSpace(quizSet.FileQuiz) && !string.IsNullOrWhiteSpace(request.FileQuiz))
						{
							quizSet.FileQuiz = request.FileQuiz;
						}
						quizSet.IsActive = true;
						quizSet.SharingStatus = request.SharingStatus;
						await _uow.QuizSets.UpdateAsync(quizSet);
					}

					questionCount = await _uow.QuizSetQuestions.CountAsync(x => x.QuizSetId == quizSet.Id && x.DeletedAt == null);
				}

				var quiz = new EventQuiz
				{
					EventId = request.EventId,
					QuizSetId = quizSet.Id,
					Title = request.Title,
					Type = request.Type,
					PassingScore = request.PassingScore,
					QuestionSetStatus = questionCount > 0 ? QuestionSetEnum.Available : QuestionSetEnum.NA,
					FileQuiz = quizSet.FileQuiz,
					LiveQuizLink = request.Type == QuizTypeEnum.LiveQuiz ? request.LiveQuizLink?.Trim() : null,
					AllowReview = request.AllowReview,
					Status = QuizStatusEnum.Draft,
					IsActive = true
				};

				await _uow.EventQuiz.CreateAsync(quiz);
				await RefreshEventQuizQuestionsAsync(quiz);
				await _uow.SaveChangesAsync();
				await transaction.CommitAsync();

				return new CreateQuizSetResponseDto
				{
					Quiz = MapQuizSummaryContract(quiz, quizSet, questionCount, 0, 0)
				};
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task<PreviewQuizResponseDto> PreviewQuizAsync(PreviewQuizRequestDto request)
		{
			await GetOwnedQuizAsync(request.QuizId, request.UserId ?? string.Empty);
			var detail = await GetQuizDetailAsync(new GetQuizDetailRequestDto { QuizId = request.QuizId });
			if (detail == null)
				throw new KeyNotFoundException("Quiz không tồn tại.");

			return new PreviewQuizResponseDto
			{
				Preview = detail
			};
		}

		public async Task<PublishQuizResponseDto> PublishQuizAsync(PublishQuizRequestDto request)
		{
			var quiz = await GetOwnedQuizAsync(request.QuizId, request.UserId ?? string.Empty, q => q.Include(x => x.EventQuizQuestions));

			if (quiz.QuestionSetStatus != QuestionSetEnum.Available || !quiz.EventQuizQuestions.Any(x => x.DeletedAt == null))
				throw new InvalidOperationException("Quiz chưa có câu hỏi để publish.");

			quiz.Status = QuizStatusEnum.Published;
			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			return new PublishQuizResponseDto
			{
				QuizId = quiz.Id,
				Status = quiz.Status
			};
		}

		public async Task<PublishQuizSetResponseDto> PublishQuizSetAsync(PublishQuizSetRequestDto request)
		{
			var quiz = await GetOwnedQuizAsync(
				request.QuizId,
				request.UserId,
				q => q.Include(x => x.QuizSet));

			if (quiz.QuizSet == null)
				throw new InvalidOperationException("Quiz chưa liên kết quiz set.");

			quiz.QuizSet.SharingStatus = QuizSetVisibilityEnum.Public;
			await _uow.QuizSets.UpdateAsync(quiz.QuizSet);
			await _uow.SaveChangesAsync();

			return new PublishQuizSetResponseDto
			{
				QuizId = quiz.Id,
				SharingStatus = quiz.QuizSet.SharingStatus
			};
		}

		public async Task<DeleteQuizResponseDto> DeleteQuizAsync(DeleteQuizRequestDto request)
		{
			var quiz = await GetOwnedQuizAsync(
				request.QuizId,
				request.UserId,
				q => q.Include(x => x.EventQuizQuestions)
					.Include(x => x.StudentQuizScores));

			if (quiz.StudentQuizScores.Any(x => x.DeletedAt == null))
				throw new InvalidOperationException("Quiz đã có lượt làm bài, không thể xóa.");

			foreach (var question in quiz.EventQuizQuestions.Where(x => x.DeletedAt == null).ToList())
			{
				await _uow.EventQuizQuestions.RemoveAsync(question);
			}

			await _uow.EventQuiz.RemoveAsync(quiz);
			await _uow.SaveChangesAsync();

			return new DeleteQuizResponseDto
			{
				QuizId = request.QuizId
			};
		}

		public async Task<CloseQuizResponseDto> CloseQuizAsync(CloseQuizRequestDto request)
		{
			var quiz = await GetOwnedQuizAsync(request.QuizId, request.UserId);
			quiz.Status = QuizStatusEnum.Closed;
			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			return new CloseQuizResponseDto
			{
				QuizId = quiz.Id,
				Status = quiz.Status
			};
		}

		public async Task<UpdateQuizQuestionResponseDto> UpdateQuizQuestionAsync(UpdateQuizQuestionRequestDto request)
		{
			var normalizedRequest = NormalizeQuestionRequest(new AddQuizQuestionRequestDto
			{
				QuizId = request.QuizId,
				QuestionText = request.QuestionText,
				Options = request.Options,
				TypeOption = request.TypeOption,
				CorrectAnswer = request.CorrectAnswer,
				Explanation = request.Explanation,
				ScorePoint = request.ScorePoint,
				Difficulty = request.Difficulty
			});

			_validator.ValidateAddQuestion(normalizedRequest);

			var question = await _uow.EventQuizQuestions.GetAsync(
				x => x.Id == request.EventQuizQuestionId,
				q => q.Include(x => x.EventQuiz));
			if (question == null)
				throw new KeyNotFoundException("Không tìm thấy câu hỏi.");
			if (question.EventQuizId != request.QuizId)
				throw new InvalidOperationException("Câu hỏi không thuộc quiz này.");

			var quiz = await GetOwnedQuizAsync(request.QuizId, request.UserId);
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz đang publish, không thể cập nhật câu hỏi.");

			question.QuestionText = normalizedRequest.QuestionText.Trim();
			question.OptionA = normalizedRequest.Options.OptionA.Trim();
			question.OptionB = normalizedRequest.Options.OptionB.Trim();
			question.OptionC = string.IsNullOrWhiteSpace(normalizedRequest.Options.OptionC) ? null : normalizedRequest.Options.OptionC.Trim();
			question.OptionD = string.IsNullOrWhiteSpace(normalizedRequest.Options.OptionD) ? null : normalizedRequest.Options.OptionD.Trim();
			question.CorrectAnswer = normalizedRequest.CorrectAnswer;
			question.Explanation = normalizedRequest.Explanation;
			question.ScorePoint = normalizedRequest.ScorePoint > 0 ? normalizedRequest.ScorePoint : 1;
			question.Difficulty = normalizedRequest.Difficulty;

			await _uow.EventQuizQuestions.UpdateAsync(question);

			if (!string.IsNullOrWhiteSpace(question.QuestionBankId))
			{
				var questionBank = await _uow.QuestionBanks.GetByIdAsync(question.QuestionBankId);
				if (questionBank != null)
				{
					questionBank.QuestionText = question.QuestionText;
					questionBank.OptionA = question.OptionA;
					questionBank.OptionB = question.OptionB;
					questionBank.OptionC = question.OptionC;
					questionBank.OptionD = question.OptionD;
					questionBank.CorrectAnswer = question.CorrectAnswer;
					questionBank.Explanation = question.Explanation;
					questionBank.Difficulty = question.Difficulty;
					await _uow.QuestionBanks.UpdateAsync(questionBank);
				}

				if (!string.IsNullOrWhiteSpace(quiz.QuizSetId))
				{
					var quizSetQuestion = await _uow.QuizSetQuestions.GetAsync(x => x.QuizSetId == quiz.QuizSetId && x.QuestionBankId == question.QuestionBankId);
					if (quizSetQuestion != null)
					{
						quizSetQuestion.ScorePoint = question.ScorePoint;
						await _uow.QuizSetQuestions.UpdateAsync(quizSetQuestion);
					}
				}
			}

			await _uow.SaveChangesAsync();

			return new UpdateQuizQuestionResponseDto
			{
				Question = MapQuestionContract(question)
			};
		}

		public async Task<DeleteQuizQuestionResponseDto> DeleteQuizQuestionAsync(DeleteQuizQuestionRequestDto request)
		{
			var question = await _uow.EventQuizQuestions.GetAsync(x => x.Id == request.EventQuizQuestionId);
			if (question == null)
				throw new KeyNotFoundException("Không tìm thấy câu hỏi.");
			if (question.EventQuizId != request.QuizId)
				throw new InvalidOperationException("Câu hỏi không thuộc quiz này.");

			var quiz = await GetOwnedQuizAsync(request.QuizId, request.UserId);
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz đang publish, không thể xóa câu hỏi.");

			await _uow.EventQuizQuestions.RemoveAsync(question);

			if (!string.IsNullOrWhiteSpace(quiz.QuizSetId) && !string.IsNullOrWhiteSpace(question.QuestionBankId))
			{
				var quizSetQuestion = await _uow.QuizSetQuestions.GetAsync(x => x.QuizSetId == quiz.QuizSetId && x.QuestionBankId == question.QuestionBankId);
				if (quizSetQuestion != null)
				{
					await _uow.QuizSetQuestions.RemoveAsync(quizSetQuestion);
				}
			}

			await _uow.SaveChangesAsync();

			var remainingQuestions = await _uow.EventQuizQuestions.CountAsync(x => x.EventQuizId == request.QuizId && x.DeletedAt == null);
			quiz.QuestionSetStatus = remainingQuestions > 0 ? QuestionSetEnum.Available : QuestionSetEnum.NA;
			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			return new DeleteQuizQuestionResponseDto
			{
				QuizId = request.QuizId,
				EventQuizQuestionId = request.EventQuizQuestionId,
				RemainingQuestions = remainingQuestions
			};
		}

		public async Task<AddQuizQuestionResponseDto> AddQuizQuestionAsync(AddQuizQuestionRequestDto request)
		{
			request = NormalizeQuestionRequest(request);
			_validator.ValidateAddQuestion(request);
			var quiz = await _uow.EventQuiz.GetAsync(
				q => q.Id == request.QuizId,
				q => q.Include(x => x.Event).Include(x => x.QuizSet));
			if (quiz == null)
				throw new KeyNotFoundException("Quiz set not found");
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz Ä‘Ã£ publish, khÃ´ng thá»ƒ thÃªm cÃ¢u há»i");
			if (quiz.QuizSet == null || string.IsNullOrWhiteSpace(quiz.Event?.OrganizerId))
				throw new InvalidOperationException("Quiz set chÆ°a Ä‘Æ°á»£c cáº¥u hÃ¬nh Ä‘áº§y Ä‘á»§.");

			var questionBank = new QuestionBank
			{
				TopicId = quiz.QuizSet.TopicId,
				OrganizerId = quiz.Event.OrganizerId,
				QuestionText = request.QuestionText.Trim(),
				OptionA = request.Options.OptionA.Trim(),
				OptionB = request.Options.OptionB.Trim(),
				OptionC = string.IsNullOrWhiteSpace(request.Options.OptionC) ? null : request.Options.OptionC.Trim(),
				OptionD = string.IsNullOrWhiteSpace(request.Options.OptionD) ? null : request.Options.OptionD.Trim(),
				CorrectAnswer = request.CorrectAnswer,
				Explanation = request.Explanation,
				Difficulty = request.Difficulty
			};

			await _uow.QuestionBanks.CreateAsync(questionBank);
			var orderIndex = await _uow.QuizSetQuestions.CountAsync(x => x.QuizSetId == quiz.QuizSetId && x.DeletedAt == null) + 1;
			var quizSetQuestion = new QuizSetQuestion
			{
				QuizSetId = quiz.QuizSetId!,
				QuestionBankId = questionBank.Id,
				ScorePoint = request.ScorePoint > 0 ? request.ScorePoint : 1,
				OrderIndex = orderIndex
			};

			await _uow.QuizSetQuestions.CreateAsync(quizSetQuestion);
			await _uow.EventQuizQuestions.CreateAsync(CreateSnapshotQuestion(quiz, questionBank, orderIndex, quizSetQuestion.ScorePoint ?? 1));
			quiz.QuestionSetStatus = QuestionSetEnum.Available;
			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();
			quizSetQuestion.QuestionBank = questionBank;
			await SyncQuestionBankAsync(quiz.QuizSet, QuestionSetEnum.Available);
			await _uow.SaveChangesAsync();

			return new AddQuizQuestionResponseDto
			{
				Question = MapQuestionContract(quizSetQuestion)
			};
		}

		public async Task<GetQuizDetailResponseDto?> GetQuizDetailAsync(GetQuizDetailRequestDto request)
		{
			var quiz = await _uow.EventQuiz.GetAsync(
				q => q.Id == request.QuizId,
				q => q.Include(x => x.QuizSet)
					.Include(x => x.Event)
						.ThenInclude(x => x!.Semester)
					.Include(x => x.StudentQuizScores)
					.Include(x => x.EventQuizQuestions));
			if (quiz == null)
				return null;

			var questions = quiz.EventQuizQuestions
				.Where(x => x.DeletedAt == null)
				.OrderBy(x => x.OrderIndex)
				.Select(MapQuestionContract)
				.ToList();

			var attemptCount = quiz.StudentQuizScores.Count;
			var passedCount = quiz.StudentQuizScores.Count(x => (x.Score ?? 0) >= (quiz.PassingScore ?? 0));

			return new GetQuizDetailResponseDto
			{
				Quiz = MapQuizSummaryContract(quiz, quiz.QuizSet, questions.Count, attemptCount, passedCount),
				Questions = questions
			};
		}

		public async Task<GetOrganizerQuizzesResponseDto> GetOrganizerQuizzesAsync(GetOrganizerQuizzesRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(request.UserId))
				throw new InvalidOperationException("Không xác định được organizer hiện tại.");

			var organizer = await GetOrganizerAsync(request.UserId);
			var organizerEvents = (await _uow.Events.GetAllAsync(
				x => x.OrganizerId == organizer.Id
					&& x.DeletedAt == null
					&& (string.IsNullOrWhiteSpace(request.EventId) || x.Id == request.EventId)))
				.ToList();

			if (!organizerEvents.Any())
			{
				return new GetOrganizerQuizzesResponseDto();
			}

			var eventIds = organizerEvents.Select(x => x.Id).ToList();
			var quizzes = (await _uow.EventQuiz.GetAllAsync(
				x => eventIds.Contains(x.EventId) && x.DeletedAt == null,
				q => q.Include(x => x.QuizSet)
					.Include(x => x.Event)
						.ThenInclude(x => x!.Semester)
					.Include(x => x.StudentQuizScores)
					.Include(x => x.EventQuizQuestions)))
				.OrderByDescending(x => x.CreatedAt)
				.ToList();

			return new GetOrganizerQuizzesResponseDto
			{
				Quizzes = quizzes.Select(quiz =>
				{
					var questionCount = quiz.EventQuizQuestions.Count(x => x.DeletedAt == null);
					var attemptCount = quiz.StudentQuizScores.Count(x => x.DeletedAt == null);
					var passedCount = quiz.StudentQuizScores.Count(x => x.DeletedAt == null && (x.Score ?? 0) >= (quiz.PassingScore ?? 0));
					return MapQuizSummaryContract(quiz, quiz.QuizSet, questionCount, attemptCount, passedCount);
				}).ToList()
			};
		}

		public async Task<GetQuizScoresResponseDto> GetQuizScoresAsync(GetQuizScoresRequestDto request)
		{
			var quiz = await _uow.EventQuiz.GetAsync(q => q.Id == request.QuizId, inc => inc.Include(x => x.StudentQuizScores));
			return new GetQuizScoresResponseDto
			{
				QuizId = request.QuizId,
				Scores = quiz?.StudentQuizScores.Select(MapQuizScoreContract).ToList() ?? new List<QuizScoreContract>()
			};
		}

		public async Task<GetStudentQuizScoreResponseDto?> GetStudentQuizScoreAsync(GetStudentQuizScoreRequestDto request)
		{
			var quiz = await _uow.EventQuiz.GetAsync(q => q.Id == request.QuizId, inc => inc.Include(x => x.StudentQuizScores));
			if (quiz == null)
				return null;

			var score = quiz.StudentQuizScores.FirstOrDefault(x => x.StudentId == request.StudentId);
			return new GetStudentQuizScoreResponseDto
			{
				QuizId = request.QuizId,
				Score = score == null ? null : MapQuizScoreContract(score)
			};
		}

		public async Task<UpdateQuizSetResponseDto> UpdateQuizSetAsync(UpdateQuizSetRequestDto request)
		{
			_validator.ValidateUpdateQuizSet(request);

			var quiz = await GetOwnedQuizAsync(
				request.QuizId,
				request.UserId,
				q => q.Include(x => x.QuizSet)
					.Include(x => x.StudentQuizScores)
					.Include(x => x.EventQuizQuestions));
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz Ä‘ang má»Ÿ, khÃ´ng thá»ƒ chá»‰nh sá»­a");
			if (quiz.EventId != request.EventId)
				throw new InvalidOperationException("Quiz khÃ´ng thuá»™c event nÃ y");

			var eventData = await _uow.Events.GetByIdAsync(request.EventId);
			if (eventData == null)
				throw new ArgumentException("Event khÃ´ng tá»“n táº¡i.");
			if (eventData.StartTime <= DateTime.UtcNow)
				throw new Exception("Sá»± kiá»‡n Ä‘Ã£ báº¯t Ä‘áº§u, khÃ´ng thá»ƒ cáº­p nháº­t Quiz");

			quiz.Title = request.Title;
			quiz.Type = request.Type;
			quiz.PassingScore = request.PassingScore;
			quiz.TimeLimit = request.TimeLimit;
			quiz.LiveQuizLink = request.Type == QuizTypeEnum.LiveQuiz ? request.LiveQuizLink?.Trim() : null;
			quiz.AllowReview = request.AllowReview;
			if (quiz.QuizSet != null)
			{
				quiz.QuizSet.Title = request.Title;
				quiz.QuizSet.TopicId = request.TopicId ?? quiz.QuizSet.TopicId ?? eventData.TopicId;
				if (!string.IsNullOrWhiteSpace(request.FileQuiz))
				{
					quiz.QuizSet.FileQuiz = request.FileQuiz;
				}
				await _uow.QuizSets.UpdateAsync(quiz.QuizSet);
			}

			await _uow.EventQuiz.UpdateAsync(quiz);
			await _uow.SaveChangesAsync();

			var questionCount = quiz.EventQuizQuestions.Count(x => x.DeletedAt == null);
			var attemptCount = quiz.StudentQuizScores.Count(x => x.DeletedAt == null);
			var passedCount = quiz.StudentQuizScores.Count(x => (x.Score ?? 0) >= (quiz.PassingScore ?? 0));

			return new UpdateQuizSetResponseDto
			{
				Quiz = MapQuizSummaryContract(quiz, quiz.QuizSet, questionCount, attemptCount, passedCount)
			};
		}

		public async Task<UploadQuizFileResponseDto> UploadQuizFileAsync(UploadQuizFileRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(request.FileName))
				throw new ArgumentException("fileName khÃ´ng há»Ÿp lá»‡");
			if (request.FileContent == null || request.FileContent.Length == 0)
				throw new ArgumentException("fileContent khÃ´ng há»Ÿp lá»‡");

			var organizer = await GetOrganizerAsync(request.UserId);
			var quiz = await _uow.EventQuiz.GetAsync(
				q => q.Id == request.QuizId,
				q => q.Include(x => x.Event).Include(x => x.QuizSet));
			if (quiz == null)
				throw new KeyNotFoundException("Quiz set not found");
			if (quiz.Event?.OrganizerId != organizer.Id)
				throw new InvalidOperationException("Báº¡n khÃ´ng cÃ³ quyá»n upload file cho quiz nÃ y.");
			if (quiz.QuizSet == null)
				throw new InvalidOperationException("Quiz chÆ°a liÃªn káº¿t quiz set.");
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz Ä‘Ã£ publish, khÃ´ng thá»ƒ upload láº¡i file.");

			var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
			var allowExt = new[] { ".pdf", ".txt", ".docx" };
			if (!allowExt.Contains(ext))
				throw new InvalidOperationException("Chá»‰ cho phÃ©p file PDF, TXT hoáº·c DOCX");

			var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "quiz", quiz.QuizSet.Id);
			Directory.CreateDirectory(uploadsRoot);
			var safeFileName = Path.GetFileName(request.FileName);
			var fullPath = Path.Combine(uploadsRoot, safeFileName);
			await File.WriteAllBytesAsync(fullPath, request.FileContent);

			string textContent = ext switch
			{
				".txt" => await File.ReadAllTextAsync(fullPath),
				".docx" => ReadDocx(fullPath),
				".pdf" => ReadPdf(fullPath),
				_ => string.Empty
			};

			var questions = ParseQuestions(textContent);
			if (!questions.Any())
				throw new InvalidOperationException("KhÃ´ng Ä‘á»c Ä‘Æ°á»£c cÃ¢u há»i há»£p lá»‡ tá»« file quiz.");

			var relative = Path.Combine("uploads", "quiz", quiz.QuizSet.Id, safeFileName).Replace('\\', '/');
			quiz.QuizSet.FileQuiz = relative;
			quiz.QuizSet.TopicId ??= quiz.Event?.TopicId;
			quiz.QuizSet.OrganizerId ??= organizer.Id;

			using var transaction = await _uow.BeginTransactionAsync();
			try
			{
				await PersistQuestionsAsync(quiz.QuizSet, organizer, questions, true);
				await RefreshEventQuizQuestionsAsync(quiz);
				await _uow.QuizSets.UpdateAsync(quiz.QuizSet);
				await _uow.EventQuiz.UpdateAsync(quiz);
				await _uow.SaveChangesAsync();
				await transaction.CommitAsync();

				return new UploadQuizFileResponseDto
				{
					QuizId = request.QuizId,
					FileQuiz = relative,
					ImportedQuestionCount = questions.Count
				};
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		private string ReadDocx(string path)
		{
			using var doc = WordprocessingDocument.Open(path, false);
			var body = doc.MainDocumentPart?.Document?.Body;
			if (body == null)
			{
				return string.Empty;
			}

			var paragraphs = body.Descendants<Paragraph>()
				.Select(p => p.InnerText?.Trim())
				.Where(p => !string.IsNullOrWhiteSpace(p));

			return string.Join(Environment.NewLine, paragraphs);
		}

		private string ReadPdf(string path)
		{
			var text = new StringBuilder();
			using var pdf = PdfDocument.Open(path);
			foreach (var page in pdf.GetPages())
			{
				text.AppendLine(page.Text);
			}

			return text.ToString();
		}

		private List<QuizQuestionContract> ParseQuestions(string text)
		{
			var questions = new List<QuizQuestionContract>();
			if (string.IsNullOrWhiteSpace(text))
			{
				return questions;
			}

			var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
			var blocks = Regex.Split(normalizedText, @"(?=^\s*Question\s*:)", RegexOptions.Multiline | RegexOptions.IgnoreCase)
				.Where(b => !string.IsNullOrWhiteSpace(b))
				.ToList();

			if (!blocks.Any() && normalizedText.Contains("Question:", StringComparison.OrdinalIgnoreCase))
			{
				blocks = normalizedText.Split("Question:", StringSplitOptions.RemoveEmptyEntries)
					.Select(b => $"Question:{b}")
					.ToList();
			}

			foreach (var block in blocks)
			{
				var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				var options = new QuizQuestionOptionContract();
				string questionText = string.Empty;
				string? correctAnswer = null;
				int score = 1;

				foreach (var line in lines)
				{
					if (line.StartsWith("Question", StringComparison.OrdinalIgnoreCase))
					{
						var parts = line.Split(':', 2);
						questionText = parts.Length == 2 ? parts[1].Trim() : line.Trim();
						continue;
					}

					if (TryReadOption(line, 'A', out var optionA))
					{
						options.OptionA = optionA;
						continue;
					}
					if (TryReadOption(line, 'B', out var optionB))
					{
						options.OptionB = optionB;
						continue;
					}
					if (TryReadOption(line, 'C', out var optionC))
					{
						options.OptionC = optionC;
						continue;
					}
					if (TryReadOption(line, 'D', out var optionD))
					{
						options.OptionD = optionD;
						continue;
					}
					if (line.StartsWith("Answer", StringComparison.OrdinalIgnoreCase))
					{
						var parts = line.Split(':', 2);
						correctAnswer = NormalizeAnswer(parts.Length == 2 ? parts[1] : line);
						continue;
					}
					if (line.StartsWith("Score", StringComparison.OrdinalIgnoreCase))
					{
						var parts = line.Split(':', 2);
						if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var parsedScore) && parsedScore > 0)
						{
							score = parsedScore;
						}
					}
				}

				if (string.IsNullOrWhiteSpace(questionText) || string.IsNullOrWhiteSpace(options.OptionA) || string.IsNullOrWhiteSpace(options.OptionB) || string.IsNullOrWhiteSpace(correctAnswer))
				{
					continue;
				}

				questions.Add(new QuizQuestionContract
				{
					QuestionText = questionText,
					Options = options,
					CorrectAnswer = correctAnswer,
					ScorePoint = score,
					Difficulty = QuestionDifficultyEnum.Medium
				});
			}

			return questions;
		}
	}
}
