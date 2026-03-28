using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Chat.Chatbot;
using BusinessLogic.Helper;
using BusinessLogic.Service.UserActivities;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Service.Chat
{
	/// <summary>
	/// Service để xử lý logic chatbot và gọi RAG API
	/// </summary>
	public class ChatbotService : IChatbotService
	{
		private readonly HttpClient _httpClient;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<ChatbotService> _logger;
		private readonly IConfiguration _configuration;
		private readonly string _ragApiBaseUrl;
		private readonly JsonSerializerOptions _jsonOptions;
		private readonly IUserActivityLogService _userActivityLogService;
		private readonly IServiceScopeFactory _scopeFactory;

		public ChatbotService(
			HttpClient httpClient, 
			IUnitOfWork unitOfWork, 
			IHttpContextAccessor httpContextAccessor, 
			ILogger<ChatbotService> logger, 
			IConfiguration configuration,
			IUserActivityLogService userActivityLogService,
			IServiceScopeFactory scopeFactory)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_userActivityLogService = userActivityLogService ?? throw new ArgumentNullException(nameof(userActivityLogService));
			_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
			
			// Đọc URL từ nhiều nguồn config khác nhau (Azure App Settings hoặc appsettings.json)
			_ragApiBaseUrl = (_configuration["AppSettings:AiEngineUrl"]
				?? _configuration["AppSettings__AiEngineUrl"]
				?? _configuration["AiEngineUrl"]
				?? _configuration["RagApi:BaseUrl"])
				?.TrimEnd('/') ?? "http://localhost:8000";
			
			_jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		/// <summary>
		/// Gửi câu hỏi tới RAG API và lấy câu trả lời
		/// </summary>
		public async Task<ChatbotResponseDto> AskQuestionAsync(string question, int topK = 5, string? sessionId = null, string? userId = null, string? role = null)
		{
			var effectiveUserId = userId ?? GetCurrentUserId();
			var effectiveUserRole = Enum.TryParse<RoleEnum>(role, true, out var r) ? r : GetCurrentUserRole();
			string effectiveSessionId = sessionId ?? "error";

			try
			{
				string userProfileContext = string.Empty;

				// Step 1: Build basic profile context with an isolated scope
				userProfileContext = await BuildRequesterProfileContextAsync(effectiveUserId, effectiveUserRole);

				// Step 2: Inject Personalization Context (Behavioral learning)
				using (var scope = _scopeFactory.CreateScope())
				{
					var ualService = scope.ServiceProvider.GetRequiredService<IUserActivityLogService>();
					var userBehaviorContext = await ualService.GetUserPersonalizationContextAsync(effectiveUserId);
					if (!string.IsNullOrWhiteSpace(userBehaviorContext))
					{
						userProfileContext = string.IsNullOrWhiteSpace(userProfileContext)
							? userBehaviorContext
							: $"{userProfileContext}\n{userBehaviorContext}";
					}

					// Log the activity to DB
					await ualService.LogActivityAsync(
						userId: effectiveUserId,
						actionType: UserActionType.USER_ASKED_CHATBOT,
						description: question.Length > 500 ? question[..500] : question
					);
				}

				// Step 3: Handle Session in its own scope
				var activeSession = await GetOrCreateActiveSessionAsync(effectiveUserId, sessionId);
				effectiveSessionId = activeSession.Id;

				// Step 4: Build Dynamic Content (Events, schedule, etc.)
				var dynamicRetrievalContext = await BuildDynamicRetrievalContextAsync(effectiveUserId, effectiveUserRole, question, effectiveSessionId);
				if (!string.IsNullOrWhiteSpace(dynamicRetrievalContext))
				{
					userProfileContext = string.IsNullOrWhiteSpace(userProfileContext)
						? dynamicRetrievalContext
						: $"{userProfileContext}\n\n{dynamicRetrievalContext}";
				}

				_logger.LogInformation($"[ChatbotService] Asking question: '{question}', sessionId: {effectiveSessionId}");

				var requestPayload = new
				{
					question,
					top_k = topK,
					role = MapRoleForRag(effectiveUserRole),
					session_id = effectiveSessionId,
					user_id = effectiveUserId,
					user_profile_context = userProfileContext,
					save_to_db = false
				};

				var json = JsonSerializer.Serialize(requestPayload);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				_logger.LogDebug($"[ChatbotService] Sending request to RAG API: {json}");

				var response = await _httpClient.PostAsync($"{_ragApiBaseUrl}/ask", content);

				_logger.LogInformation($"[ChatbotService] RAG API returned status: {response.StatusCode}");

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError($"[ChatbotService] RAG API error {response.StatusCode}: {errorContent}");
					await SaveConversationAsync(effectiveSessionId, effectiveUserId, effectiveUserRole, question, "Lỗi khi gọi RAG API", ChatMessageStatus.Error, errorContent);
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Lỗi khi gọi RAG API",
						SessionId = effectiveSessionId,
						Answer = null
					};
				}

				var responseJson = await response.Content.ReadAsStringAsync();
				_logger.LogDebug($"[ChatbotService] RAG API raw response: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

				var apiResponse = JsonSerializer.Deserialize<RagApiResponse>(responseJson, _jsonOptions);

				if (apiResponse == null)
				{
					_logger.LogError("[ChatbotService] Failed to deserialize RAG API response");
					await SaveConversationAsync(effectiveSessionId, effectiveUserId, effectiveUserRole, question, "Không nhận được dữ liệu từ RAG API", ChatMessageStatus.Error, "Deserialize response failed");
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Không nhận được dữ liệu từ RAG API",
						SessionId = effectiveSessionId,
						Answer = null
					};
				}

				_logger.LogInformation($"[ChatbotService] Deserialized response: Answer length={apiResponse.Answer?.Length ?? 0}, Sources count={apiResponse.Sources?.Length ?? 0}, Error='{apiResponse.Error}'");

				if (!string.IsNullOrWhiteSpace(apiResponse.Error))
				{
					_logger.LogWarning($"[ChatbotService] RAG API returned error: {apiResponse.Error}");
					await SaveConversationAsync(effectiveSessionId, effectiveUserId, effectiveUserRole, question, apiResponse.Error, ChatMessageStatus.Error, apiResponse.Error);
					return new ChatbotResponseDto
					{
						Success = false,
						Message = apiResponse.Error,
						SessionId = apiResponse.SessionId ?? effectiveSessionId,
						Answer = null
					};
				}

				var finalAnswer = string.IsNullOrWhiteSpace(apiResponse.Answer)
					? "Mình chưa tìm thấy thông tin phù hợp trong dữ liệu hiện tại. Bạn thử hỏi cụ thể hơn về tên sự kiện, thời gian hoặc địa điểm nhé."
					: apiResponse.Answer;

				_logger.LogInformation($"[ChatbotService] Final answer: '{finalAnswer.Substring(0, Math.Min(100, finalAnswer.Length))}...'");
				var returnedSessionId = string.IsNullOrWhiteSpace(apiResponse.SessionId)
					? effectiveSessionId
					: apiResponse.SessionId!;
				await SaveConversationAsync(returnedSessionId, effectiveUserId, effectiveUserRole, question, finalAnswer, ChatMessageStatus.Final);

				return new ChatbotResponseDto
				{
					Success = true,
					Message = "Thành công",
					SessionId = returnedSessionId,
					Answer = finalAnswer,
					Sources = apiResponse.Sources != null ? apiResponse.Sources.Select(s => new SourceDto
					{
						Score = ParseScoreToFloat(s.Score),
						Meta = s.Meta
					}).ToArray() : Array.Empty<SourceDto>()
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[ChatbotService] CRITICAL Error in AskQuestionAsync: {ex.Message}");

				// If it's a DbContext concurrency error, don't try to use the same context to save the error
				// because it will likely fail again with the same "second operation started" message.
				var isContextError = ex is InvalidOperationException && ex.Message.Contains("DbContext");

				try
				{
					var fallbackUserId = effectiveUserId;
					var fallbackUserRole = effectiveUserRole;
					var fallbackSession = await GetOrCreateActiveSessionAsync(fallbackUserId, sessionId);

					if (!isContextError)
					{
						await SaveConversationAsync(fallbackSession.Id, fallbackUserId, fallbackUserRole, question, $"Lỗi hệ thống: {ex.Message}", ChatMessageStatus.Error, ex.Message);
					}
				}
				catch (Exception logEx)
				{
					_logger.LogWarning($"[ChatbotService] Could not save error conversation to DB: {logEx.Message}");
				}

				return new ChatbotResponseDto
				{
					Success = false,
					Message = isContextError
						? "Hệ thống đang bận xử lý dữ liệu sự kiện. Vui lòng thử lại sau giây lát."
						: $"Lỗi: {ex.Message}",
					SessionId = sessionId ?? "error",
					Answer = null
				};
			}
		}

		/// <summary>
		/// Kiểm tra trạng thái RAG API server
		/// </summary>
		public async Task<ChatbotConversationHistoryDto> GetConversationHistoryAsync(string? sessionId = null, int limit = 100)
		{
			var userId = GetCurrentUserId();
			var safeLimit = Math.Clamp(limit, 1, 200);

			using (var scope = _scopeFactory.CreateScope())
			{
				var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				ChatbotSession? session = null;
				if (!string.IsNullOrWhiteSpace(sessionId))
				{
					session = await uow.ChatbotSessions.GetAsync(
						s => s.Id == sessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);
				}

				if (session == null)
				{
					session = await uow.ChatbotSessions.GetAsync(
						s => s.UserId == userId && s.Status == ChatSessionStatus.Active,
						q => q.OrderByDescending(s => s.StartedAt));
				}

				if (session == null)
				{
					return new ChatbotConversationHistoryDto
					{
						SessionId = null,
						Messages = new List<ChatbotHistoryMessageDto>()
					};
				}

				var messages = (await uow.ChatbotMessages.GetAllAsync(
					m => m.SessionId == session.Id,
					q => q.OrderByDescending(m => m.CreatedAt).Take(safeLimit)))
					.OrderBy(m => m.CreatedAt)
					.Select(m => new ChatbotHistoryMessageDto
					{
						Sender = m.Sender,
						Content = m.Content,
						CreatedAt = m.CreatedAt
					})
					.ToList();

				return new ChatbotConversationHistoryDto
				{
					SessionId = session.Id,
					Messages = messages
				};
			}
		}

		public async Task<HealthStatusDto> CheckHealthAsync()
		{
			try
			{
				var response = await _httpClient.GetAsync($"{_ragApiBaseUrl}/health");

				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					return new HealthStatusDto
					{
						IsHealthy = true,
						Status = "ok",
						Message = "RAG API đang hoạt động"
					};
				}
				else
				{
					return new HealthStatusDto
					{
						IsHealthy = false,
						Status = "error",
						Message = "RAG API không phản hồi"
					};
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Health check failed: {ex.Message}", ex);
				return new HealthStatusDto
				{
					IsHealthy = false,
					Status = "error",
					Message = $"Không thể kết nối tới RAG API: {ex.Message}"
				};
			}
		}

		private static float ParseScoreToFloat(string? score)
		{
			if (string.IsNullOrWhiteSpace(score))
			{
				return 0f;
			}

			var trimmed = score.Trim().Replace("%", "");
			if (float.TryParse(trimmed, out var percent))
			{
				return percent / 100f;
			}

			return 0f;
		}

		private string GetCurrentUserId()
		{
			var user = _httpContextAccessor.HttpContext?.User;
			if (user == null) return "anonymous";

			return user.GetUserId() ?? "anonymous";
		}

		private RoleEnum GetCurrentUserRole()
		{
			var user = _httpContextAccessor.HttpContext?.User;
			if (user == null) return RoleEnum.Student;

			var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
			if (!string.IsNullOrWhiteSpace(roleClaim) && Enum.TryParse<RoleEnum>(roleClaim, true, out var role))
			{
				return role;
			}

			return RoleEnum.Student;
		}

		private static string MapRoleForRag(RoleEnum role)
		{
			return role switch
			{
				RoleEnum.Admin => "admin",
				RoleEnum.Organizer => "staff",
				RoleEnum.Approver => "staff",
				RoleEnum.Student => "student",
				_ => "user",
			};
		}

		private static bool IsPendingApprovalQuestion(string question)
		{
			var normalized = NormalizeIntentText(question);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			var hasApprovalIntent =
				normalized.Contains("duyet")
				|| normalized.Contains("duoc duyet")
				|| normalized.Contains("pending")
				|| normalized.Contains("cho duyet")
				|| normalized.Contains("gui duyet")
				|| normalized.Contains("phe duyet")
				|| normalized.Contains("duoc gui")
				|| normalized.Contains("gui cho toi")
				|| normalized.Contains("event gui")
				|| normalized.Contains("approve")
				|| normalized.Contains("approval")
				|| normalized.Contains("Pending");
			var hasEventContext =
				normalized.Contains("su kien")
				|| normalized.Contains("sukien")
				|| normalized.Contains("event")
				|| normalized.Contains("Event");
			return hasApprovalIntent && hasEventContext;
		}

		private static bool IsOrganizerMyEventsQuestion(string question)
		{
			var normalized = NormalizeIntentText(question);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			var hasMyEventContext =
				normalized.Contains("my event")
				|| normalized.Contains("su kien cua toi")
				|| normalized.Contains("su kien to chuc")
				|| normalized.Contains("event cua toi")
				|| normalized.Contains("myevent");

			var hasEventWord =
				normalized.Contains("su kien")
				|| normalized.Contains("event");

			var hasListingIntent =
				normalized.Contains("co")
				|| normalized.Contains("bao nhieu")
				|| normalized.Contains("liet ke")
				|| normalized.Contains("thong tin")
				|| normalized.Contains("gi")
				|| normalized.Contains("nao");

			return hasMyEventContext || (hasEventWord && hasListingIntent && normalized.Contains("toi"));
		}

		private static bool IsEventQuestion(string question)
		{
			var normalized = NormalizeIntentText(question);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}
			return normalized.Contains("su kien") || normalized.Contains("event");
		}

		private enum StudentScheduleScope
		{
			Today,
			ThisWeek,
			Upcoming,
		}

		private static bool IsStudentScheduleQuestion(string question, out StudentScheduleScope scope)
		{
			scope = StudentScheduleScope.Today;
			var normalized = NormalizeIntentText(question);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			var asksEvent = normalized.Contains("su kien") || normalized.Contains("event");
			var asksToday = normalized.Contains("hom nay") || normalized.Contains("today");
			var asksThisWeek = normalized.Contains("tuan nay") || normalized.Contains("this week");
			var asksUpcoming = normalized.Contains("sap toi") || normalized.Contains("upcoming") || normalized.Contains("sap dien ra");

			if (!asksEvent || (!asksToday && !asksThisWeek && !asksUpcoming))
			{
				return false;
			}

			if (asksUpcoming)
			{
				scope = StudentScheduleScope.Upcoming;
			}
			else
			{
				scope = asksThisWeek ? StudentScheduleScope.ThisWeek : StudentScheduleScope.Today;
			}
			return true;
		}

		private static string NormalizeIntentText(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return string.Empty;
			}

			var formD = value.Normalize(NormalizationForm.FormD);
			var builder = new StringBuilder(formD.Length);
			foreach (var c in formD)
			{
				var category = CharUnicodeInfo.GetUnicodeCategory(c);
				if (category != UnicodeCategory.NonSpacingMark)
				{
					builder.Append(char.ToLowerInvariant(c));
				}
			}

			var withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC);
			return Regex.Replace(withoutDiacritics, "\\s+", " ").Trim();
		}


		private async Task<string> BuildApproverPendingAnswerAsync(IUnitOfWork uow, string approverUserId)
		{
			string? approverStaffId = null;
			if (!string.IsNullOrWhiteSpace(approverUserId) && approverUserId != "anonymous")
			{
				approverStaffId = (await uow.StaffProfiles.GetAsync(s => s.UserId == approverUserId))?.Id;
			}

			var pendingEvents = await uow.Events.GetAllAsync(
				e => e.Status == EventStatusEnum.Pending
					&& e.DeletedAt == null
					&& (approverStaffId == null || e.OrganizerId != approverStaffId),
				q => q.Include(e => e.Organizer!).ThenInclude(o => o!.User));

			if (pendingEvents == null || !pendingEvents.Any())
			{
				return "Hiện tại không có sự kiện nào đang chờ duyệt.";
			}

			var topPending = pendingEvents
				.OrderByDescending(e => e.UpdatedAt)
				.ThenBy(e => e.StartTime)
				.Take(5)
				.Select(e =>
				{
					var organizerName = e.Organizer?.User?.FullName;
					var startTimeText = e.StartTime.ToVietnamTime().ToString("dd/MM/yyyy HH:mm");
					if (!string.IsNullOrWhiteSpace(organizerName))
					{
						return $"- {e.Title} (Organizer: {organizerName}, Bắt đầu: {startTimeText})";
					}
					return $"- {e.Title} (Bắt đầu: {startTimeText})";
				})
				.ToList();

			return $"Hiện tại có {pendingEvents.Count()} sự kiện đang chờ duyệt. Dưới đây là 5 sự kiện mới nhất:\n{string.Join("\n", topPending)}";
		}

		private async Task<string> BuildOrganizerMyEventsAnswerAsync(IUnitOfWork uow, string organizerUserId)
		{
			string? organizerStaffId = null;
			if (!string.IsNullOrWhiteSpace(organizerUserId) && organizerUserId != "anonymous")
			{
				organizerStaffId = (await uow.StaffProfiles.GetAsync(s => s.UserId == organizerUserId))?.Id;
			}

			if (string.IsNullOrWhiteSpace(organizerStaffId))
			{
				return "Không xác định được hồ sơ Organizer của bạn để lấy danh sách sự kiện.";
			}

			var myEvents = await uow.Events.GetAllAsync(
				e => e.OrganizerId == organizerStaffId && e.DeletedAt == null,
				q => q
					.Include(e => e.Location)
					.OrderByDescending(e => e.StartTime));

			if (myEvents == null || !myEvents.Any())
			{
				return "Hiện tại bạn chưa tổ chức sự kiện nào.";
			}

			var statusSummary = myEvents
				.GroupBy(e => e.Status)
				.OrderByDescending(g => g.Count())
				.Select(g => $"{g.Key}: {g.Count()}")
				.ToList();

			var details = myEvents
				.OrderByDescending(e => e.StartTime)
				.Take(5)
				.Select(e =>
				{
					var location = !string.IsNullOrWhiteSpace(e.Location?.Name) ? e.Location.Name : "Chưa cập nhật";
					var startText = e.StartTime.ToVietnamTime().ToString("dd/MM/yyyy HH:mm");
					var endText = e.EndTime.ToVietnamTime().ToString("dd/MM/yyyy HH:mm");
					var modeText = e.Mode?.ToString() ?? "Chưa cập nhật";
					var typeText = e.Type?.ToString() ?? "Chưa cập nhật";
					return $"- {e.Title} | Trạng thái: {e.Status} | Loại: {typeText} | Hình thức: {modeText} | Thời gian: {startText} -> {endText} | Địa điểm: {location}";
				})
				.ToList();

			return
				$"My Event của bạn hiện có {myEvents.Count()} sự kiện.\n"
				+ $"Tổng quan trạng thái: {string.Join(", ", statusSummary)}\n"
				+ "Chi tiết:\n"
				+ string.Join("\n", details);
		}

		private static bool IsStudentScheduleFollowUpQuestion(string question)
		{
			var normalized = NormalizeIntentText(question);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			return normalized.Contains("con")
				|| normalized.Contains("khac")
				|| normalized.Contains("nua")
				|| normalized.Contains("van")
				|| normalized.Contains("co nua khong")
				|| normalized.Contains("con su kien nao")
				|| normalized.Contains("con nao khong")
				|| normalized.Contains("a vay");
		}

		private async Task<StudentScheduleScope?> ResolveStudentScheduleScopeAsync(IUnitOfWork uow, string sessionId, string question)
		{
			if (IsStudentScheduleQuestion(question, out var directScope))
			{
				return directScope;
			}

			if (!IsStudentScheduleFollowUpQuestion(question) || string.IsNullOrWhiteSpace(sessionId))
			{
				return null;
			}

			var recentMessages = await uow.ChatbotMessages.GetAllAsync(
				m => m.SessionId == sessionId,
				q => q.OrderByDescending(m => m.CreatedAt).Take(20));

			foreach (var msg in recentMessages)
			{
				if (!string.Equals(msg.Sender, "user", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (string.IsNullOrWhiteSpace(msg.Content) || string.Equals(msg.Content.Trim(), question.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (IsStudentScheduleQuestion(msg.Content, out var previousScope))
				{
					return previousScope;
				}
			}

			return null;
		}

		private async Task<string> BuildDynamicRetrievalContextAsync(string userId, RoleEnum role, string question, string sessionId)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var sections = new List<string>();

				if (role == RoleEnum.Approver && IsPendingApprovalQuestion(question))
				{
					var pendingContext = await BuildApproverPendingAnswerAsync(uow, userId);
					if (!string.IsNullOrWhiteSpace(pendingContext))
					{
						sections.Add($"[Dữ liệu phê duyệt trực tiếp từ DB]\n{pendingContext}");
					}
				}

				if (role == RoleEnum.Organizer && IsOrganizerMyEventsQuestion(question))
				{
					var organizerContext = await BuildOrganizerMyEventsAnswerAsync(uow, userId);
					if (!string.IsNullOrWhiteSpace(organizerContext))
					{
						sections.Add($"[Dữ liệu My Event trực tiếp từ DB]\n{organizerContext}");
					}
				}

				if (role == RoleEnum.Student && (IsEventQuestion(question) || IsStudentScheduleFollowUpQuestion(question)))
				{
					var resolvedScope = await ResolveStudentScheduleScopeAsync(uow, sessionId, question);
					var effectiveScope = resolvedScope ?? StudentScheduleScope.ThisWeek;
					var studentScheduleContext = await BuildStudentScheduleAnswerAsync(uow, userId, effectiveScope);
					if (!string.IsNullOrWhiteSpace(studentScheduleContext))
					{
						sections.Add($"[Dữ liệu lịch trực tiếp từ DB]\n{studentScheduleContext}");
					}
				}

				if (!sections.Any())
				{
					return string.Empty;
				}

				return "Ngữ cảnh truy xuất động (ưu tiên cao, lấy trực tiếp từ DB):\n" + string.Join("\n\n", sections);
			}
		}

		private async Task<string> BuildStudentScheduleAnswerAsync(IUnitOfWork uow, string studentUserId, StudentScheduleScope scope)
		{
			var now = DateTimeHelper.VietnamNow;
			var endWindow = now.AddDays(14);

			var publishedEvents = await uow.Events.GetAllAsync(
				e => e.DeletedAt == null && e.Status == EventStatusEnum.Published,
				q => q
					.Include(e => e.Location)
					.OrderBy(e => e.StartTime));

			var events = (publishedEvents ?? Enumerable.Empty<DataAccess.Entities.Event>()).ToList();

			if (!events.Any())
			{
				return "Hiện tại chưa có lịch sự kiện nào sắp tới.";
			}

			var payload = new
			{
				source = "system_database_published_events",
				scope = scope.ToString(),
				reference_time = now.ToString("yyyy-MM-dd HH:mm:ss"),
				total = events.Count,
				events = events.Select(e => new
				{
					title = e.Title,
					status = e.Status.ToString(),
					start_time = e.StartTime.ToVietnamTime().ToString("yyyy-MM-dd HH:mm:ss"),
					end_time = e.EndTime.ToVietnamTime().ToString("yyyy-MM-dd HH:mm:ss"),
					location = !string.IsNullOrWhiteSpace(e.Location?.Name) ? e.Location!.Name : "Chưa cập nhật",
					mode = e.Mode?.ToString(),
					type = e.Type?.ToString(),
				}),
			};

			return JsonSerializer.Serialize(payload);
		}

		private async Task<string> BuildRequesterProfileContextAsync(string userId, RoleEnum role)
		{
			if (string.IsNullOrWhiteSpace(userId) || userId == "anonymous")
			{
				return string.Empty;
			}

			using (var scope = _scopeFactory.CreateScope())
			{
				try
				{
					var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
					var user = await uow.Users.GetAsync(
						u => u.Id == userId,
						q => q
							.Include(u => u.Role)
							.Include(u => u.StudentProfile)
								.ThenInclude(sp => sp!.Department)
							.Include(u => u.StaffProfile)
								.ThenInclude(sp => sp!.Department)
					);

					if (user == null)
					{
						return string.Empty;
					}

					var lines = new List<string>
					{
						$"- Họ tên: {user.FullName}",
						$"- Vai trò hệ thống: {role}",
						$"- Email: {user.Email}",
					};

					if (!string.IsNullOrWhiteSpace(user.Phone))
					{
						lines.Add($"- Số điện thoại: {user.Phone}");
					}

					if (user.StudentProfile != null)
					{
						lines.Add("- Nhóm người dùng: Student");
						if (!string.IsNullOrWhiteSpace(user.StudentProfile.StudentCode))
						{
							lines.Add($"- Mã sinh viên: {user.StudentProfile.StudentCode}");
						}
						if (!string.IsNullOrWhiteSpace(user.StudentProfile.CurrentSemester))
						{
							lines.Add($"- Học kỳ hiện tại: {user.StudentProfile.CurrentSemester}");
						}
						if (!string.IsNullOrWhiteSpace(user.StudentProfile.Department?.Name))
						{
							lines.Add($"- Khoa/Bộ môn: {user.StudentProfile.Department.Name}");
						}
					}

					if (user.StaffProfile != null)
					{
						lines.Add("- Nhóm người dùng: Staff");
						if (!string.IsNullOrWhiteSpace(user.StaffProfile.StaffCode))
						{
							lines.Add($"- Mã nhân sự: {user.StaffProfile.StaffCode}");
						}
						if (!string.IsNullOrWhiteSpace(user.StaffProfile.Position))
						{
							lines.Add($"- Chức vụ: {user.StaffProfile.Position}");
						}
						if (!string.IsNullOrWhiteSpace(user.StaffProfile.Department?.Name))
						{
							lines.Add($"- Khoa/Bộ môn: {user.StaffProfile.Department.Name}");
						}
					}

					return string.Join("\n", lines);
				}
				catch (Exception ex)
				{
					_logger.LogWarning($"[ChatbotService] Failed to build requester profile context: {ex.Message}");
					return string.Empty;
				}
			}
		}

		private async Task<ChatbotSession> GetOrCreateActiveSessionAsync(string userId, string? requestedSessionId)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				ChatbotSession? session = null;

				if (!string.IsNullOrWhiteSpace(requestedSessionId))
				{
					session = await uow.ChatbotSessions.GetAsync(
						s => s.Id == requestedSessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);
				}

				if (session == null)
				{
					session = await uow.ChatbotSessions.GetAsync(
						s => s.UserId == userId && s.Status == ChatSessionStatus.Active,
						q => q.OrderByDescending(s => s.StartedAt));
				}

				if (session != null)
				{
					return session;
				}

				session = new ChatbotSession
				{
					Id = requestedSessionId ?? Guid.NewGuid().ToString(),
					UserId = userId,
					StartedAt = DateTimeHelper.VietnamNow,
					Status = ChatSessionStatus.Active
				};

				await uow.ChatbotSessions.CreateAsync(session);
				await uow.SaveChangesAsync();
				return session;
			}
		}

		private async Task SaveConversationAsync(string sessionId, string userId, RoleEnum userRole, string question, string answer, ChatMessageStatus assistantStatus, string? errorMessage = null)
		{
			using (var scope = _scopeFactory.CreateScope())
			{
				try
				{
					var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
					var session = await uow.ChatbotSessions.GetAsync(
						s => s.Id == sessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);

					if (session == null)
					{
						session = await uow.ChatbotSessions.GetAsync(
							s => s.UserId == userId && s.Status == ChatSessionStatus.Active,
							q => q.OrderByDescending(s => s.StartedAt));
					}

					if (session == null)
					{
						session = new ChatbotSession
						{
							Id = sessionId,
							UserId = userId,
							StartedAt = DateTimeHelper.VietnamNow,
							Status = ChatSessionStatus.Active
						};
						await uow.ChatbotSessions.CreateAsync(session);
					}

					var userMessage = new ChatbotMessage
					{
						SessionId = session.Id,
						Sender = "user",
						Content = question,
						Role = userRole,
						Status = ChatMessageStatus.Final,
						CreatedAt = DateTimeHelper.VietnamNow
					};

					var assistantMessage = new ChatbotMessage
					{
						SessionId = session.Id,
						Sender = "assistant",
						Content = answer,
						Role = userRole,
						Status = assistantStatus,
						ErrorMessage = errorMessage,
						CreatedAt = DateTimeHelper.VietnamNow.AddMilliseconds(10)
					};

					await uow.ChatbotMessages.CreateAsync(userMessage);
					await uow.ChatbotMessages.CreateAsync(assistantMessage);
					await uow.SaveChangesAsync();
				}
				catch (Exception ex)
				{
					_logger.LogError($"[ChatbotService] Failed to save chatbot conversation: {ex.Message}", ex);
				}
			}
		}
	}
}
