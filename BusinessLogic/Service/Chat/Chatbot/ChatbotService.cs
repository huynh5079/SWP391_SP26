using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using BusinessLogic.DTOs.Chat.Chatbot;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

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
		private readonly string _ragApiBaseUrl;
		private readonly JsonSerializerOptions _jsonOptions;

        public ChatbotService(HttpClient httpClient, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<ChatbotService> logger)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
			_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_ragApiBaseUrl = "http://localhost:8000"; 
			_jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		/// <summary>
		/// Gửi câu hỏi tới RAG API và lấy câu trả lời
		/// </summary>
		public async Task<ChatbotResponseDto> AskQuestionAsync(string question, int topK = 5, string? sessionId = null)
		{
			try
			{
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
				var userProfileContext = await BuildRequesterProfileContextAsync(userId, userRole);

				if (string.IsNullOrWhiteSpace(question))
				{
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Câu hỏi không được để trống",
						Answer = null
					};
				}

				var activeSession = await GetOrCreateActiveSessionAsync(userId, sessionId);
				var effectiveSessionId = activeSession.Id;

				if (userRole == RoleEnum.Approver && IsPendingApprovalQuestion(question))
				{
					var pendingAnswer = await BuildApproverPendingAnswerAsync(userId);
					await SaveConversationAsync(effectiveSessionId, userId, userRole, question, pendingAnswer, ChatMessageStatus.Final);
					return new ChatbotResponseDto
					{
						Success = true,
						Message = "Thành công",
						SessionId = effectiveSessionId,
						Answer = pendingAnswer,
						Sources = Array.Empty<SourceDto>()
					};
				}

				_logger.LogInformation($"[ChatbotService] Asking question: '{question}', topK: {topK}, sessionId: {effectiveSessionId}, RAG URL: {_ragApiBaseUrl}/ask");

				var request = new
				{
					question,
					top_k = topK,
					role = MapRoleForRag(userRole),
					session_id = effectiveSessionId,
					user_id = userId,
					user_profile_context = userProfileContext,
					save_to_db = false
				};
				var json = JsonSerializer.Serialize(request);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				_logger.LogDebug($"[ChatbotService] Sending request to RAG API: {json}");

				var response = await _httpClient.PostAsync($"{_ragApiBaseUrl}/ask", content);

				_logger.LogInformation($"[ChatbotService] RAG API returned status: {response.StatusCode}");

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError($"[ChatbotService] RAG API error {response.StatusCode}: {errorContent}");
                   await SaveConversationAsync(effectiveSessionId, userId, userRole, question, "Lỗi khi gọi RAG API", ChatMessageStatus.Error, errorContent);
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
                   await SaveConversationAsync(effectiveSessionId, userId, userRole, question, "Không nhận được dữ liệu từ RAG API", ChatMessageStatus.Error, "Deserialize response failed");
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
                   await SaveConversationAsync(effectiveSessionId, userId, userRole, question, apiResponse.Error, ChatMessageStatus.Error, apiResponse.Error);
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
				await SaveConversationAsync(returnedSessionId, userId, userRole, question, finalAnswer, ChatMessageStatus.Final);

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
				_logger.LogError($"[ChatbotService] Error in AskQuestionAsync: {ex.Message}\n{ex.StackTrace}", ex);
               var userId = GetCurrentUserId();
				var userRole = GetCurrentUserRole();
				var fallbackSession = await GetOrCreateActiveSessionAsync(userId, sessionId);
				await SaveConversationAsync(fallbackSession.Id, userId, userRole, question, $"Lỗi: {ex.Message}", ChatMessageStatus.Error, ex.Message);
				return new ChatbotResponseDto
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					SessionId = fallbackSession.Id,
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

			ChatbotSession? session = null;
			if (!string.IsNullOrWhiteSpace(sessionId))
			{
				session = await _unitOfWork.ChatbotSessions.GetAsync(
					s => s.Id == sessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);
			}

			if (session == null)
			{
				session = await _unitOfWork.ChatbotSessions.GetAsync(
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

			var messages = (await _unitOfWork.ChatbotMessages.GetAllAsync(
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

        public async Task<HealthStatusDto> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_ragApiBaseUrl}/health");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var health = JsonSerializer.Deserialize<dynamic>(json, _jsonOptions);
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
			return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? "anonymous";
		}

		private RoleEnum GetCurrentUserRole()
		{
			var roleClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
			return Enum.TryParse<RoleEnum>(roleClaim, true, out var role)
				? role
				: RoleEnum.Student;
		}

		private static string MapRoleForRag(RoleEnum role)
		{
			return role switch
			{
				RoleEnum.Admin => "admin",
				RoleEnum.Organizer => "staff",
				RoleEnum.Approver => "staff",
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

		private async Task<string> BuildApproverPendingAnswerAsync(string approverUserId)
		{
			string? approverStaffId = null;
			if (!string.IsNullOrWhiteSpace(approverUserId) && approverUserId != "anonymous")
			{
				approverStaffId = (await _unitOfWork.StaffProfiles.GetAsync(s => s.UserId == approverUserId))?.Id;
			}

			var pendingEvents = await _unitOfWork.Events.GetAllAsync(
				e => e.Status == EventStatusEnum.Pending
					&& e.DeletedAt == null
					&& (approverStaffId == null || e.OrganizerId != approverStaffId),
				q => q.Include(e => e.Organizer!).ThenInclude(o => o!.User));

			if (pendingEvents == null || !pendingEvents.Any())
			{
				return "Hiện tại không có sự kiện nào được gửi đến bạn để duyệt.";
			}

			var topPending = pendingEvents
				.OrderByDescending(e => e.UpdatedAt)
				.ThenBy(e => e.StartTime)
				.Take(3)
				.Select(e =>
				{
					var organizerName = e.Organizer?.User?.FullName;
					var startTimeText = e.StartTime.ToString("dd/MM/yyyy HH:mm");
					if (!string.IsNullOrWhiteSpace(organizerName))
					{
						return $"- {e.Title} (Organizer: {organizerName}, Bắt đầu: {startTimeText})";
					}
					return $"- {e.Title} (Bắt đầu: {startTimeText})";
				})
				.ToList();

			return $"Hiện tại có {pendingEvents.Count()} sự kiện đang chờ duyệt:\n{string.Join("\n", topPending)}";
		}

		private async Task<string> BuildRequesterProfileContextAsync(string userId, RoleEnum role)
		{
			if (string.IsNullOrWhiteSpace(userId) || userId == "anonymous")
			{
				return string.Empty;
			}

			try
			{
				var user = await _unitOfWork.Users.GetAsync(
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

		private async Task<ChatbotSession> GetOrCreateActiveSessionAsync(string userId, string? requestedSessionId)
		{
			ChatbotSession? session = null;

			if (!string.IsNullOrWhiteSpace(requestedSessionId))
			{
				session = await _unitOfWork.ChatbotSessions.GetAsync(
					s => s.Id == requestedSessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);
			}

			if (session == null)
			{
				session = await _unitOfWork.ChatbotSessions.GetAsync(
					s => s.UserId == userId && s.Status == ChatSessionStatus.Active,
					q => q.OrderByDescending(s => s.StartedAt));
			}

			if (session != null)
			{
				return session;
			}

			session = new ChatbotSession
			{
				Id = string.IsNullOrWhiteSpace(requestedSessionId) ? Guid.NewGuid().ToString() : requestedSessionId,
				UserId = userId,
				StartedAt = DateTime.UtcNow,
				Status = ChatSessionStatus.Active
			};

			await _unitOfWork.ChatbotSessions.CreateAsync(session);
			await _unitOfWork.SaveChangesAsync();
			return session;
		}

		private async Task SaveConversationAsync(string sessionId, string userId, RoleEnum userRole, string question, string answer, ChatMessageStatus assistantStatus, string? errorMessage = null)
		{
			try
			{
				var session = await _unitOfWork.ChatbotSessions.GetAsync(
					s => s.Id == sessionId && s.UserId == userId && s.Status == ChatSessionStatus.Active);

				if (session == null)
				{
					session = await GetOrCreateActiveSessionAsync(userId, sessionId);
				}

				var userMessage = new ChatbotMessage
				{
					SessionId = session.Id,
					Sender = "user",
					Content = question,
					Status = ChatMessageStatus.Final,
                    Role = userRole
				};

				var assistantMessage = new ChatbotMessage
				{
					SessionId = session.Id,
					Sender = "assistant",
					Content = answer,
					Status = assistantStatus,
                    Role = userRole,
					ErrorMessage = assistantStatus == ChatMessageStatus.Error ? errorMessage : null
				};

				await _unitOfWork.ChatbotMessages.CreateAsync(userMessage);
				await _unitOfWork.ChatbotMessages.CreateAsync(assistantMessage);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError($"[ChatbotService] Failed to save chatbot conversation: {ex.Message}", ex);
			}
		}

		
	}


	
}
