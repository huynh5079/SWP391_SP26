using System;
using System.Collections;
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
		private readonly BusinessLogic.Storage.IFileStorageService _fileStorageService;

		public QuizService(IUnitOfWork uow, IQuizValidator validator, BusinessLogic.Storage.IFileStorageService fileStorageService)
		{
			_uow = uow;
			_validator = validator;
			_fileStorageService = fileStorageService;
		}

		// helper: truncate a long string for error messages
		private static string Truncate(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return string.Empty;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
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
                SemesterName = quiz.Event?.Semester?.Name.ToString() ?? quiz.Event?.Semester?.Code ?? string.Empty,
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
				QuizStartTime = quiz.QuizStartTime,
				QuizEndTime = quiz.QuizEndTime,
				AllowReview = quiz.AllowReview,
				IsActive = quiz.IsActive,
				QuestionCount = questionCount,
				AttemptCount = attemptCount,
				PassedCount = passedCount,
				MaxAttempts = quiz.MaxAttemptSubmission,
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
				throw new ArgumentException("UserId không hợp lệ.");
			}


			var staff = await _uow.StaffProfiles.GetAsync(x => x.UserId == userId);
			if (staff == null)
			{
				throw new InvalidOperationException("Chưa thiết lập hồ sơ nhân viên (StaffProfile).");
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

		private static bool TryReadInlineOptions(string line, QuizQuestionOptionContract options)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return false;
			}

			var matches = Regex.Matches(
				line,
				@"([A-D])\s*[\.:\)\-]\s*(.*?)(?=(?:\s+[A-D]\s*[\.:\)\-]\s*)|$)",
				RegexOptions.IgnoreCase);

			if (matches.Count <= 1)
			{
				return false;
			}

			foreach (Match match in matches)
			{
				var option = match.Groups[1].Value.ToUpperInvariant();
				var value = match.Groups[2].Value.Trim();
				if (string.IsNullOrWhiteSpace(value))
				{
					continue;
				}

				switch (option)
				{
					case "A": options.OptionA = value; break;
					case "B": options.OptionB = value; break;
					case "C": options.OptionC = value; break;
					case "D": options.OptionD = value; break;
				}
			}

			return !string.IsNullOrWhiteSpace(options.OptionA) && !string.IsNullOrWhiteSpace(options.OptionB);
		}

		private static string? NormalizeAnswer(string? value)
		{
           // Normalize: remove whitespace (including NBSP and tabs), uppercase and validate
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			var cleaned = value.Replace("\u00A0", " ") // NBSP
				.Replace("\t", " ")
				.Replace(" ", string.Empty)
				.ToUpperInvariant();

			// Accept formats like "A , B", "a, b" -> cleaned becomes "A,B"
			if (!Regex.IsMatch(cleaned, @"^[A-D](,[A-D])*$"))
			{
				throw new ArgumentException("Invalid answer format");
			}

			return string.Join(",", cleaned
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Distinct()
				.OrderBy(x => x));
		}

		private static string? NormalizeAnswers(string? value, QuestionTypeOptionEnum typeOption)
		{
            // Normalize individual answers first (removes spaces, normalizes case)
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
          // Improved comparison: trim + case-insensitive
			string a = optionA?.Trim() ?? string.Empty;
			string b = optionB?.Trim() ?? string.Empty;
			string c = optionC?.Trim() ?? string.Empty;
			string d = optionD?.Trim() ?? string.Empty;

			if (a.Equals("TRUE", StringComparison.OrdinalIgnoreCase) && b.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
				&& string.IsNullOrWhiteSpace(c) && string.IsNullOrWhiteSpace(d))
			{
				return QuestionTypeOptionEnum.TrueFalse;
			}

			// multiple choice if correct answer lists multiple entries
			if (!string.IsNullOrWhiteSpace(correctAnswer) && correctAnswer.Trim().Contains(','))
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

		private async Task<string> GetUniqueQuizSetTitleAsync(string organizerId, string title, string? ignoreQuizSetId = null)
		{
			var baseTitle = string.IsNullOrWhiteSpace(title) ? "Quiz Set" : title.Trim();
			var candidate = baseTitle;
			var suffix = 2;

			while (await _uow.QuizSets.CountAsync(x => x.OrganizerId == organizerId
				&& x.Title == candidate
				&& x.DeletedAt == null
				&& (string.IsNullOrWhiteSpace(ignoreQuizSetId) || x.Id != ignoreQuizSetId)) > 0)
			{
				candidate = $"{baseTitle} ({suffix++})";
			}

			return candidate;
		}

		private async Task ValidateDuplicateQuizTitleInSemesterAsync(StaffProfile organizer, DataAccess.Entities.Event eventDataForAdd, string title)
		{
			var normalizedTitle = title?.Trim();
			if (string.IsNullOrWhiteSpace(normalizedTitle))
			{
				return;
			}

			var sameTitleQuizzes = await _uow.EventQuiz.GetAllAsync(
				x => x.DeletedAt == null && x.Title == normalizedTitle,
				q => q.Include(x => x.Event));

            // Compare by EventId to avoid false positives when titles are not unique
			var isDuplicated = sameTitleQuizzes.Any(x => x.Event != null
				&& x.Event.DeletedAt == null
				&& x.Event.OrganizerId == organizer.Id
				&& x.Event.SemesterId == eventDataForAdd.SemesterId
				&& x.EventId == eventDataForAdd.Id);

			if (isDuplicated)
			{
				throw new InvalidOperationException("Đã tồn tại quiz có cùng tiêu đề cho event này trong cùng semester.");
			}
		}

		private async Task<(QuizSet QuizSet, int QuestionCount)> CreateQuizSetFromSourceAsync(QuizSet sourceQuizSet, StaffProfile organizer, CreateQuizSetRequestDto request, string? fallbackTopicId)
		{
			var uniqueQuizSetTitle = await GetUniqueQuizSetTitleAsync(organizer.Id, request.Title);
			var quizSet = new QuizSet
			{
				TopicId = request.TopicId ?? sourceQuizSet.TopicId ?? fallbackTopicId,
				OrganizerId = organizer.Id,
				Title = uniqueQuizSetTitle,
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

		private async Task<QuizSet> EnsureExclusiveQuizSetAsync(EventQuiz quiz)
		{
			if (quiz.QuizSet == null || string.IsNullOrWhiteSpace(quiz.QuizSetId))
				throw new InvalidOperationException("Quiz chưa liên kết quiz set.");

			var activeLinkedQuizCount = await _uow.EventQuiz.CountAsync(x => x.QuizSetId == quiz.QuizSetId && x.DeletedAt == null);
			if (activeLinkedQuizCount <= 1)
			{
				return quiz.QuizSet;
			}

			var sourceQuizSet = await _uow.QuizSets.GetAsync(
				x => x.Id == quiz.QuizSetId && x.DeletedAt == null,
				q => q.Include(x => x.QuizSetQuestions)
					.ThenInclude(x => x.QuestionBank));

			if (sourceQuizSet == null)
				throw new InvalidOperationException("Không tìm thấy quiz set nguồn.");

			var uniqueQuizSetTitle = await GetUniqueQuizSetTitleAsync(
				sourceQuizSet.OrganizerId ?? string.Empty,
				sourceQuizSet.Title,
				sourceQuizSet.Id);

			var clonedQuizSet = new QuizSet
			{
				TopicId = sourceQuizSet.TopicId,
				OrganizerId = sourceQuizSet.OrganizerId,
				Title = uniqueQuizSetTitle,
				Description = sourceQuizSet.Description,
				FileQuiz = sourceQuizSet.FileQuiz,
				SharingStatus = sourceQuizSet.SharingStatus,
				IsActive = sourceQuizSet.IsActive
			};

			await _uow.QuizSets.CreateAsync(clonedQuizSet);

			foreach (var sourceQuestion in sourceQuizSet.QuizSetQuestions
				.Where(x => x.DeletedAt == null && x.QuestionBank != null)
				.OrderBy(x => x.OrderIndex))
			{
				var clonedQuestionBank = CloneQuestionBank(sourceQuestion.QuestionBank!, clonedQuizSet.OrganizerId ?? string.Empty, clonedQuizSet.TopicId);
				await _uow.QuestionBanks.CreateAsync(clonedQuestionBank);

				await _uow.QuizSetQuestions.CreateAsync(new QuizSetQuestion
				{
					QuizSetId = clonedQuizSet.Id,
					QuestionBankId = clonedQuestionBank.Id,
					ScorePoint = sourceQuestion.ScorePoint ?? 1,
					OrderIndex = sourceQuestion.OrderIndex
				});
			}

			await _uow.SaveChangesAsync();

			quiz.QuizSetId = clonedQuizSet.Id;
			quiz.QuizSet = clonedQuizSet;
			quiz.FileQuiz = clonedQuizSet.FileQuiz;
			await RefreshEventQuizQuestionsAsync(quiz);
			await _uow.EventQuiz.UpdateAsync(quiz);

			return clonedQuizSet;
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
         // Batch remove existing snapshots to avoid N calls
         // Batch delete existing snapshots using UnitOfWork helper to avoid N roundtrips
			await _uow.DeleteEventQuizQuestionsAsync(quiz.Id);

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

            // Create snapshots in memory then persist in batch to reduce DB roundtrips
			var snapshots = quizSetQuestions
				.Where(x => x.QuestionBank != null)
				.Select(qsq => CreateSnapshotQuestion(quiz, qsq.QuestionBank!, qsq.OrderIndex, qsq.ScorePoint ?? 1))
				.ToList();
            if (snapshots.Any())
			{
				foreach (var snap in snapshots)
				{
					await _uow.EventQuizQuestions.CreateAsync(snap);
				}
				// persist once after creating snapshots
				await _uow.SaveChangesAsync();
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
				if (existingLinks.Any())
				{
					// remove range in-memory and persist once
					foreach (var link in existingLinks)
					{
						await _uow.QuizSetQuestions.RemoveAsync(link);
					}
					await _uow.SaveChangesAsync();
				}
			}

         var orderIndex = 1;
			var seenInUpload = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var questionDto in questions.Where(x => !string.IsNullOrWhiteSpace(x.QuestionText)))
			{
				var options = GetQuestionOptions(questionDto);
				var answer = NormalizeAnswers(questionDto.CorrectAnswer, questionDto.TypeOption) ?? string.Empty;
				if (string.IsNullOrWhiteSpace(options.OptionA) || string.IsNullOrWhiteSpace(options.OptionB))
				{
					continue;
				}
				var normalizedQuestionText = questionDto.QuestionText.Trim();
				var optionA = options.OptionA?.Trim();
				var optionB = options.OptionB?.Trim();
				var optionC = string.IsNullOrWhiteSpace(options.OptionC) ? null : options.OptionC?.Trim();
				var optionD = string.IsNullOrWhiteSpace(options.OptionD) ? null : options.OptionD?.Trim();
				var fingerprint = string.Join("||",
					normalizedQuestionText,
					optionA ?? string.Empty,
					optionB ?? string.Empty,
					optionC ?? string.Empty,
					optionD ?? string.Empty,
					answer);
				if (!seenInUpload.Add(fingerprint))
				{
					continue;
				}

				var questionBank = new QuestionBank
				{
					TopicId = quizSet.TopicId,
					OrganizerId = organizer.Id,
					QuestionText = normalizedQuestionText,
					OptionA = optionA,
					OptionB = optionB,
					OptionC = optionC,
					OptionD = optionD,
					CorrectAnswer = answer,
					Explanation = string.IsNullOrWhiteSpace(questionDto.Explanation) ? null : questionDto.Explanation.Trim(),
					Difficulty = questionDto.Difficulty
				};

				await _uow.QuestionBanks.CreateAsync(questionBank);

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
			await _uow.SaveChangesAsync();
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
				throw new Exception("Event không tồn tại");
			if (eventDataForAdd.OrganizerId != organizer.Id)
				throw new InvalidOperationException("Bạn không có quyền tạo quiz cho event này.");
			if (eventDataForAdd.StartTime <= DateTime.UtcNow)
				throw new Exception("Sự kiện đã bắt đầu, không thể tạo quiz");
			var (normalizedQuizStartTime, normalizedQuizEndTime) = NormalizeQuizSchedule(
				request.QuizStartTime,
				request.QuizEndTime,
				request.TimeLimit,
				eventDataForAdd.StartTime,
				eventDataForAdd.EndTime);
			ValidateQuizScheduleWithinEventWindow(normalizedQuizStartTime, normalizedQuizEndTime, eventDataForAdd.StartTime, eventDataForAdd.EndTime);
			await ValidateDuplicateQuizTitleInSemesterAsync(organizer, eventDataForAdd, request.Title);

			using var transaction = await _uow.BeginTransactionAsync();
			try
			{
				QuizSet quizSet;
				int questionCount;

				if (!string.IsNullOrWhiteSpace(request.SourceQuizSetId))
				{
					if (string.IsNullOrWhiteSpace(request.TopicId))
						throw new InvalidOperationException("Vui lòng chọn topic trước khi sử dụng question bank.");

					var sourceQuizSet = await _uow.QuizSets.GetAsync(
						x => x.Id == request.SourceQuizSetId && x.DeletedAt == null && x.IsActive,
						q => q.Include(x => x.QuizSetQuestions)
							.ThenInclude(x => x.QuestionBank));

					if (sourceQuizSet == null)
						throw new KeyNotFoundException("Question bank không tồn tại.");

					if (sourceQuizSet.OrganizerId != organizer.Id && sourceQuizSet.SharingStatus != QuizSetVisibilityEnum.Public)
						throw new InvalidOperationException("Bạn không có quyền sử dụng question bank này.");

					if (string.IsNullOrWhiteSpace(sourceQuizSet.TopicId) || sourceQuizSet.TopicId != request.TopicId)
						throw new InvalidOperationException("Question bank không thuộc topic đã chọn.");

					(quizSet, questionCount) = await CreateQuizSetFromSourceAsync(sourceQuizSet, organizer, request, eventDataForAdd.TopicId);
				}
				else
				{
					var uniqueQuizSetTitle = await GetUniqueQuizSetTitleAsync(organizer.Id, request.Title);
					quizSet = new QuizSet
					{
						TopicId = request.TopicId ?? eventDataForAdd.TopicId,
						OrganizerId = organizer.Id,
						Title = uniqueQuizSetTitle,
						FileQuiz = request.FileQuiz,
						SharingStatus = request.SharingStatus,
						IsActive = true
					};

					await _uow.QuizSets.CreateAsync(quizSet);

					questionCount = await _uow.QuizSetQuestions.CountAsync(x => x.QuizSetId == quizSet.Id && x.DeletedAt == null);
				}

				var quiz = new EventQuiz
				{
					EventId = request.EventId,
					QuizSetId = quizSet.Id,
					Title = request.Title,
					Type = request.Type,
					PassingScore = request.PassingScore,
					TimeLimit = request.TimeLimit,
					QuizStartTime = normalizedQuizStartTime,
					QuizEndTime = normalizedQuizEndTime,
					QuestionSetStatus = questionCount > 0 ? QuestionSetEnum.Available : QuestionSetEnum.NA,
					FileQuiz = quizSet.FileQuiz,
					LiveQuizLink = request.Type == QuizTypeEnum.LiveQuiz ? request.LiveQuizLink?.Trim() : null,
					AllowReview = request.AllowReview,
					MaxAttemptSubmission = request.MaxAttempts,
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
			var quiz = await GetOwnedQuizAsync(request.QuizId, request.UserId ?? string.Empty, q => q.Include(x => x.Event).Include(x => x.EventQuizQuestions));
			_validator.ValidatePublishQuiz(quiz);

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

			var quizSet = await EnsureExclusiveQuizSetAsync(quiz);
			quizSet.SharingStatus = request.SharingStatus;
			await _uow.QuizSets.UpdateAsync(quizSet);
			await _uow.SaveChangesAsync();

			return new PublishQuizSetResponseDto
			{
				QuizId = quiz.Id,
				SharingStatus = quizSet.SharingStatus
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
				// Only update shared QuestionBank if it belongs to current organizer. Otherwise clone and relink to avoid global data corruption.
				var questionBank = await _uow.QuestionBanks.GetByIdAsync(question.QuestionBankId);
				if (questionBank != null)
				{
					if (questionBank.OrganizerId == quiz.Event.OrganizerId)
					{
						// Safe to update in-place
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
					else
					{
                      // Clone a new QuestionBank for this organizer and re-link
						var cloned = CloneQuestionBank(questionBank, quiz.Event.OrganizerId ?? string.Empty, quiz.QuizSet?.TopicId);
						// Apply updated fields from question
						cloned.QuestionText = question.QuestionText;
						cloned.OptionA = question.OptionA;
						cloned.OptionB = question.OptionB;
						cloned.OptionC = question.OptionC;
						cloned.OptionD = question.OptionD;
						cloned.CorrectAnswer = question.CorrectAnswer;
						cloned.Explanation = question.Explanation;
						cloned.Difficulty = question.Difficulty;
						await _uow.QuestionBanks.CreateAsync(cloned);
						// Save old QuestionBankId before changing so we can find existing QuizSetQuestion
						var oldQuestionBankId = question.QuestionBankId;
						// Update EventQuizQuestion to point to cloned
						question.QuestionBankId = cloned.Id;
						await _uow.EventQuizQuestions.UpdateAsync(question);
						if (!string.IsNullOrWhiteSpace(quiz.QuizSetId))
						{
							var quizSetQuestion = await _uow.QuizSetQuestions.GetAsync(x => x.QuizSetId == quiz.QuizSetId && x.QuestionBankId == oldQuestionBankId);
							if (quizSetQuestion != null)
							{
								quizSetQuestion.QuestionBankId = cloned.Id;
								quizSetQuestion.ScorePoint = question.ScorePoint;
								await _uow.QuizSetQuestions.UpdateAsync(quizSetQuestion);
							}
						}
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
				throw new InvalidOperationException("Quiz đã publish, không thể thêm câu hỏi");
			if (quiz.QuizSet == null || string.IsNullOrWhiteSpace(quiz.Event?.OrganizerId))
				throw new InvalidOperationException("Quiz set chưa được cấu hình đầy đủ.");

			var quizSet = await EnsureExclusiveQuizSetAsync(quiz);

			var questionBank = new QuestionBank
			{
				TopicId = quizSet.TopicId,
				OrganizerId = quiz.Event.OrganizerId,
                QuestionText = request.QuestionText?.Trim(),
				OptionA = request.Options.OptionA?.Trim(),
				OptionB = request.Options.OptionB?.Trim(),
				OptionC = string.IsNullOrWhiteSpace(request.Options.OptionC) ? null : request.Options.OptionC?.Trim(),
				OptionD = string.IsNullOrWhiteSpace(request.Options.OptionD) ? null : request.Options.OptionD?.Trim(),
				CorrectAnswer = request.CorrectAnswer,
				Explanation = request.Explanation,
				Difficulty = request.Difficulty
			};

         await _uow.QuestionBanks.CreateAsync(questionBank);

            // Load existing QuizSetQuestions once and compute max OrderIndex (avoids multiple DB calls)
			var existingQuizSetQuestions = (await _uow.QuizSetQuestions.GetAllAsync(x => x.QuizSetId == quiz.QuizSetId && x.DeletedAt == null)).ToList();
			var maxIndex = existingQuizSetQuestions.Select(x => (int?)x.OrderIndex).Max() ?? 0;
			var orderIndex = maxIndex + 1;
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
			await SyncQuestionBankAsync(quizSet, QuestionSetEnum.Available);
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
			if (quiz.EventId != request.EventId)
				throw new InvalidOperationException("Quiz không thuộc event này");

			var eventData = await _uow.Events.GetByIdAsync(request.EventId);
			if (eventData == null)
				throw new ArgumentException("Event không tồn tại.");
			if (quiz.Status != QuizStatusEnum.Published && eventData.StartTime <= DateTime.UtcNow)
				throw new Exception("Sự kiện đã bắt đầu, không thể cập nhật Quiz");

			var (normalizedQuizStartTime, normalizedQuizEndTime) = NormalizeQuizSchedule(
				request.QuizStartTime ?? quiz.QuizStartTime,
				request.QuizEndTime ?? quiz.QuizEndTime,
				request.TimeLimit,
				eventData.StartTime,
				eventData.EndTime);
			ValidateQuizScheduleWithinEventWindow(normalizedQuizStartTime, normalizedQuizEndTime, eventData.StartTime, eventData.EndTime);

			if (quiz.Status == QuizStatusEnum.Published)
			{
				if (quiz.TimeLimit.HasValue && request.TimeLimit.HasValue && request.TimeLimit.Value < quiz.TimeLimit.Value)
					throw new InvalidOperationException("Quiz đang mở, chỉ có thể tăng Time limit để gia hạn.");

				if (quiz.QuizEndTime.HasValue && normalizedQuizEndTime.HasValue && normalizedQuizEndTime.Value < quiz.QuizEndTime.Value)
					throw new InvalidOperationException("Quiz đang mở, chỉ có thể gia hạn thêm thời gian kết thúc.");
			}

			quiz.Title = request.Title;
			quiz.Type = request.Type;
			quiz.PassingScore = request.PassingScore;
			quiz.TimeLimit = request.TimeLimit;
			quiz.QuizStartTime = normalizedQuizStartTime;
			quiz.QuizEndTime = normalizedQuizEndTime;
			quiz.LiveQuizLink = request.Type == QuizTypeEnum.LiveQuiz ? request.LiveQuizLink?.Trim() : null;
			quiz.AllowReview = request.AllowReview;
			quiz.MaxAttemptSubmission = request.MaxAttempts;
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

		private static DateTime? ToUtcIfSpecified(DateTime? value)
		{
			if (!value.HasValue)
				return null;

			if (value.Value.Kind == DateTimeKind.Utc)
				return value.Value;

			if (value.Value.Kind == DateTimeKind.Local)
				return value.Value.ToUniversalTime();

			return DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime();
		}

		private static (DateTime? QuizStartTime, DateTime? QuizEndTime) NormalizeQuizSchedule(
			DateTime? quizStartTime,
			DateTime? quizEndTime,
			int? timeLimit,
			DateTime eventStartTime,
			DateTime? eventEndTime)
		{
			var normalizedQuizStart = ToUtcIfSpecified(quizStartTime);
			var normalizedQuizEnd = ToUtcIfSpecified(quizEndTime);
			var normalizedEventStart = eventStartTime.Kind == DateTimeKind.Utc ? eventStartTime : eventStartTime.ToUniversalTime();

			if (timeLimit.HasValue && timeLimit.Value > 0)
			{
				normalizedQuizStart ??= DateTime.UtcNow;
				if (normalizedQuizStart.Value < normalizedEventStart)
				{
					normalizedQuizStart = normalizedEventStart;
				}

				var minimumEndByLimit = normalizedQuizStart.Value.AddMinutes(timeLimit.Value);
				if (!normalizedQuizEnd.HasValue || normalizedQuizEnd.Value < minimumEndByLimit)
				{
					normalizedQuizEnd = minimumEndByLimit;
				}
			}

			return (normalizedQuizStart, normalizedQuizEnd);
		}

		private static void ValidateQuizScheduleWithinEventWindow(DateTime? quizStartTime, DateTime? quizEndTime, DateTime eventStartTime, DateTime? eventEndTime)
		{
			var normalizedQuizStart = ToUtcIfSpecified(quizStartTime);
			var normalizedQuizEnd = ToUtcIfSpecified(quizEndTime);
			var normalizedEventStart = eventStartTime.Kind == DateTimeKind.Utc ? eventStartTime : eventStartTime.ToUniversalTime();
			var normalizedEventEnd = eventEndTime.HasValue
				? (eventEndTime.Value.Kind == DateTimeKind.Utc ? eventEndTime.Value : eventEndTime.Value.ToUniversalTime())
				: (DateTime?)null;

			if (normalizedQuizStart.HasValue && normalizedQuizEnd.HasValue && normalizedQuizEnd <= normalizedQuizStart)
				throw new ArgumentException("Thời gian kết thúc quiz phải lớn hơn thời gian bắt đầu.");

			if (normalizedQuizStart.HasValue && normalizedQuizStart < normalizedEventStart)
				throw new ArgumentException("Thời gian bắt đầu quiz phải >= thời gian bắt đầu sự kiện.");

			if (normalizedEventEnd.HasValue && normalizedQuizEnd.HasValue && normalizedQuizEnd > normalizedEventEnd)
				throw new ArgumentException("Thời gian kết thúc quiz phải <= thời gian kết thúc sự kiện.");
		}


		public async Task<UploadQuizFileResponseDto> UploadQuizFileAsync(UploadQuizFileRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(request.FileName))
				throw new ArgumentException("fileName không hợp lệ");
			if (request.FileContent == null || request.FileContent.Length == 0)
				throw new ArgumentException("fileContent không hợp lệ");

			var organizer = await GetOrganizerAsync(request.UserId);
			var quiz = await _uow.EventQuiz.GetAsync(
				q => q.Id == request.QuizId,
				q => q.Include(x => x.Event).Include(x => x.QuizSet));
			if (quiz == null)
				throw new KeyNotFoundException("Không tìm Quiz set");
			if (quiz.Event?.OrganizerId != organizer.Id)
				throw new InvalidOperationException("Bạn không có quyền upload file cho quiz này.");
			if (quiz.QuizSet == null)
				throw new InvalidOperationException("Quiz chưa liên kết quiz set.");
			if (quiz.Status == QuizStatusEnum.Published)
				throw new InvalidOperationException("Quiz đã publish, không thể upload lại file.");

			var quizSet = await EnsureExclusiveQuizSetAsync(quiz);

           // Validate by extension and basic file signature where possible
			var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
			var allowExt = new[] { ".pdf", ".txt", ".docx" };
			if (!allowExt.Contains(ext))
				throw new InvalidOperationException("Chỉ cho phép file PDF, TXT hoặc DOCX");

			// Basic signature checks
			if (request.FileContent.Length >= 4)
			{
				// PDF: 25 50 44 46 => %PDF
				if (ext == ".pdf")
				{
					if (!(request.FileContent[0] == 0x25 && request.FileContent[1] == 0x50 && request.FileContent[2] == 0x44 && request.FileContent[3] == 0x46))
						throw new InvalidOperationException("File PDF không hợp lệ (signature mismatch)");
				}
				// DOCX is a ZIP archive; check PK header: 50 4B 03 04
				if (ext == ".docx")
				{
					if (!(request.FileContent[0] == 0x50 && request.FileContent[1] == 0x4B && request.FileContent[2] == 0x03 && request.FileContent[3] == 0x04))
						throw new InvalidOperationException("File DOCX không hợp lệ (signature mismatch)");
				}
				// TXT: allow any but ensure not binary gibberish (simple heuristic)
				if (ext == ".txt")
				{
					// check for null bytes
					if (request.FileContent.Take(128).Any(b => b == 0))
						throw new InvalidOperationException("File TXT không hợp lệ");
				}
			}

			// 1. Upload to Cloudinary using the new Stream-based method
			using var ms = new MemoryStream(request.FileContent);
			var uploadResult = await _fileStorageService.UploadSingleAsync(
				ms,
				request.FileName,
				DataAccess.Enum.UploadContext.Material, // Using Material context for Quiz files
				organizer.UserId);

			if (uploadResult == null)
				throw new InvalidOperationException("Lỗi khi upload file lên Cloudinary.");

			// 2. Parse content from memory (no local file saving needed)
			using var parseStream = new MemoryStream(request.FileContent);
			string textContent = ext switch
			{
				".txt" => await new StreamReader(parseStream).ReadToEndAsync(),
				".docx" => ReadDocxFromStream(parseStream),
				".pdf" => ReadPdfFromStream(parseStream),
				_ => string.Empty
			};

			var questions = ParseQuestions(textContent);
			if (!questions.Any())
				throw new InvalidOperationException("Không đọc được câu hỏi hợp lệ từ file quiz.");

			var relative = uploadResult.Url;
			quizSet.FileQuiz = relative;
			quizSet.TopicId ??= quiz.Event?.TopicId;
			quizSet.OrganizerId ??= organizer.Id;

			using var transaction = await _uow.BeginTransactionAsync();
			try
			{
				await PersistQuestionsAsync(quizSet, organizer, questions, true);
				await _uow.SaveChangesAsync();
				await RefreshEventQuizQuestionsAsync(quiz);

				quiz.FileQuiz = quizSet.FileQuiz;

				await _uow.QuizSets.UpdateAsync(quizSet);
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

		private string ReadDocxFromStream(Stream stream)
		{
			using var doc = WordprocessingDocument.Open(stream, false);
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

		private string ReadPdfFromStream(Stream stream)
		{
			var text = new StringBuilder();
			using var pdf = PdfDocument.Open(stream);
			foreach (var page in pdf.GetPages())
			{
				text.AppendLine(page.Text);
			}

			return text.ToString();
		}

		private List<QuizQuestionContract> ParseQuestions(string text)
		{
           // Refactored parsing:
			// - use robust splitting of potential question blocks
			// - collect parsing errors (but return valid questions)
			var questions = new List<QuizQuestionContract>();
			if (string.IsNullOrWhiteSpace(text))
				return questions;

			text = NormalizeQuizText(text);

			// Regex to split into blocks by question-like headings (supports flexible formats)
         // Fixed regex: removed stray space and accept formats like "1)" or "1." as question delimiters
			var splitRegex = new Regex(@"(?im)(?=(?:^|\n)\s*(?:câu\s*\d+|question\s*\d+|q\s*\d+|#\s*\d+|\d+\)|\d+\.))");
			var blocks = splitRegex.Split(text).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

			// Patterns used inside each block
			var optionRegex = new Regex(@"(?im)^\s*([A-D])\s*[\.\:\)\-]\s*(.+)$", RegexOptions.Multiline);
			var answerRegex = new Regex(@"(?im)(?:đáp án|answer|correct|ans)\s*[:\-]?\s*([A-D](?:\s*,\s*[A-D])*)");
			var scoreRegex = new Regex(@"(?im)(?:score|point|điểm)\s*[:\-]?\s*(\d+(?:\.\d+)?)");
			var explanationRegex = new Regex(@"(?im)(?:explanation|giải thích|note)\s*[:\-]?\s*(.+)");

			var parsingErrors = new List<string>();

			foreach (var block in blocks)
			{
				try
				{
					var body = block.Trim();
					// Extract options
					var optionMatches = optionRegex.Matches(body);
					if (optionMatches.Count < 2)
					{
						// try inline options e.g. "A. ... B. ..."
						var inlineOptions = new QuizQuestionOptionContract();
						if (!TryReadInlineOptions(body, inlineOptions))
						{
							parsingErrors.Add($"Insufficient options in block: '{Truncate(body,80)}'");
							continue;
						}
						// inlineOptions found -> treat questionText as everything before first option label
						var firstOptIndex = 0; // options are inline, questionText may be empty -> skip if empty
						var qText = body;
						if (string.IsNullOrWhiteSpace(qText))
						{
							parsingErrors.Add($"Empty question text for inline options block: '{Truncate(body,80)}'");
							continue;
						}
						var answerMatchInline = answerRegex.Match(body);
                      var answerValInline = answerMatchInline.Success ? NormalizeAnswer(answerMatchInline.Groups[1].Value) : null;
						var scoreValInline = 1.0;
						var scoreMatchInline = scoreRegex.Match(body);
						if (scoreMatchInline.Success) double.TryParse(scoreMatchInline.Groups[1].Value, out scoreValInline);
						var expMatchInline = explanationRegex.Match(body);
						var explanationInline = expMatchInline.Success ? expMatchInline.Groups[1].Value.Trim() : null;
                      questions.Add(new QuizQuestionContract
						{
							QuestionText = qText,
							Options = inlineOptions,
                          CorrectAnswer = answerValInline,
							ScorePoint = (int)Math.Max(1, Math.Round(scoreValInline)),
							Explanation = explanationInline,
							Difficulty = QuestionDifficultyEnum.Medium
						});
						continue;
					}

					// Question text is everything before the first option label
					var firstOption = optionMatches[0];
					var questionText = body.Substring(0, firstOption.Index).Trim();
					if (string.IsNullOrWhiteSpace(questionText))
					{
						parsingErrors.Add($"Empty question text in block: '{Truncate(body,80)}'");
						continue;
					}

					var options = new QuizQuestionOptionContract();
					foreach (Match option in optionMatches)
					{
						var key = option.Groups[1].Value.ToUpperInvariant();
						var value = option.Groups[2].Value.Trim();
						var ansMatchInOption = answerRegex.Match(value);
						if (ansMatchInOption.Success)
							value = value.Substring(0, ansMatchInOption.Index).Trim();
						switch (key)
						{
							case "A": options.OptionA = value; break;
							case "B": options.OptionB = value; break;
							case "C": options.OptionC = value; break;
							case "D": options.OptionD = value; break;
						}
					}

					if (string.IsNullOrWhiteSpace(options.OptionA) || string.IsNullOrWhiteSpace(options.OptionB))
					{
						parsingErrors.Add($"Missing mandatory options A/B in block: '{Truncate(questionText,80)}'");
						continue;
					}

					// if one of C/D present, both must be present
					if (!string.IsNullOrWhiteSpace(options.OptionC) ^ !string.IsNullOrWhiteSpace(options.OptionD))
					{
						parsingErrors.Add($"Incomplete C/D options in block: '{Truncate(questionText,80)}'");
						continue;
					}

					// Find Answer
					var answerMatch = answerRegex.Match(body);
					string? answerVal = null;
					if (answerMatch.Success)
					{
						answerVal = NormalizeAnswer(answerMatch.Groups[1].Value);
					}
					if (string.IsNullOrWhiteSpace(answerVal))
					{
						parsingErrors.Add($"Missing/invalid answer in question: '{Truncate(questionText,80)}'");
						continue;
					}

					// Score
					double score = 1.0;
					var scoreMatch = scoreRegex.Match(body);
					if (scoreMatch.Success)
						double.TryParse(scoreMatch.Groups[1].Value, out score);

					// Explanation
					string? explanation = null;
					var expMatch = explanationRegex.Match(body);
					if (expMatch.Success)
						explanation = expMatch.Groups[1].Value.Trim();

					questions.Add(new QuizQuestionContract
					{
						QuestionText = questionText,
						Options = options,
						CorrectAnswer = answerVal,
						ScorePoint = (int)Math.Max(1, Math.Round(score)),
						Explanation = explanation,
						Difficulty = QuestionDifficultyEnum.Medium
					});
				}
				catch (Exception ex)
				{
					parsingErrors.Add($"Exception parsing block: '{Truncate(block,80)}' -> {ex.Message}");
				}
			}

			if (questions.Count == 0 && parsingErrors.Any()) 
			{
				throw new InvalidOperationException("Không đọc được câu hỏi. Chi tiết: " + string.Join(" | ", parsingErrors));
			}

			return RemoveDuplicateQuestions(questions);
		}
		private string NormalizeQuizText(string text)
		{
			text = text.Replace("\r\n", "\n").Replace('\r', '\n');

			text = Regex.Replace(text, @"\u00A0", " "); // remove non breaking space
			text = Regex.Replace(text, @"\t", " ");

			// đảm bảo option xuống dòng - only apply when question-like patterns present to avoid accidental formatting changes
			// chỉ áp dụng nếu có pattern câu hỏi
			if (Regex.IsMatch(text, @"Câu\s*\d+|Question\s*\d+", RegexOptions.IgnoreCase))
			{
				text = Regex.Replace(text, @"\s+([A-D][\.\:\)\-])", "\n$1");
			}
			// đảm bảo question xuống dòng
			text = Regex.Replace(text, @"\s(Câu\s*\d+)", "\n$1", RegexOptions.IgnoreCase);
			text = Regex.Replace(text, @"\s(Question\s*\d+)", "\n$1", RegexOptions.IgnoreCase);

			return text.Trim();
		}
		private List<QuizQuestionContract> RemoveDuplicateQuestions(List<QuizQuestionContract> questions)
		{
			var result = new List<QuizQuestionContract>();
			var seen = new HashSet<string>();

			foreach (var q in questions)
			{
				if (string.IsNullOrWhiteSpace(q.QuestionText))
					continue;

				var key = q.QuestionText.Trim().ToLower();

				if (seen.Contains(key))
					continue;

				seen.Add(key);
				result.Add(q);
			}

			return result;
		}
	}
	public class MatchCollectionWrapper
	{
		public List<Match> Matches { get; } = new();

		public MatchCollectionWrapper(string[] blocks)
		{
			foreach (var block in blocks)
			{
				if (string.IsNullOrWhiteSpace(block)) continue;

				var fakeMatch = Regex.Match("1: " + block, @"(?<body>.+)", RegexOptions.Singleline);
				Matches.Add(fakeMatch);
			}
		}
	}
}
