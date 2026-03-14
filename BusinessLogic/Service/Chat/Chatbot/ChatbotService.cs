using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Chat.Chatbot;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.Http;
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
		public async Task<ChatbotResponseDto> AskQuestionAsync(string question, int topK = 5)
		{
			try
			{
                var userId = GetCurrentUserId();

				if (string.IsNullOrWhiteSpace(question))
				{
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Câu hỏi không được để trống",
						Answer = null
					};
				}

				_logger.LogInformation($"[ChatbotService] Asking question: '{question}', topK: {topK}, RAG URL: {_ragApiBaseUrl}/ask");

				var request = new { question, top_k = topK, role = "user" };
				var json = JsonSerializer.Serialize(request);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				_logger.LogDebug($"[ChatbotService] Sending request to RAG API: {json}");

				var response = await _httpClient.PostAsync($"{_ragApiBaseUrl}/ask", content);

				_logger.LogInformation($"[ChatbotService] RAG API returned status: {response.StatusCode}");

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					_logger.LogError($"[ChatbotService] RAG API error {response.StatusCode}: {errorContent}");
                   await SaveConversationAsync(userId, question, "Lỗi khi gọi RAG API", ChatMessageStatus.Error, errorContent);
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Lỗi khi gọi RAG API",
						Answer = null
					};
				}

				var responseJson = await response.Content.ReadAsStringAsync();
				_logger.LogDebug($"[ChatbotService] RAG API raw response: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

				var apiResponse = JsonSerializer.Deserialize<RagApiResponse>(responseJson, _jsonOptions);

				if (apiResponse == null)
				{
					_logger.LogError("[ChatbotService] Failed to deserialize RAG API response");
                   await SaveConversationAsync(userId, question, "Không nhận được dữ liệu từ RAG API", ChatMessageStatus.Error, "Deserialize response failed");
					return new ChatbotResponseDto
					{
						Success = false,
						Message = "Không nhận được dữ liệu từ RAG API",
						Answer = null
					};
				}

				_logger.LogInformation($"[ChatbotService] Deserialized response: Answer length={apiResponse.Answer?.Length ?? 0}, Sources count={apiResponse.Sources?.Length ?? 0}, Error='{apiResponse.Error}'");

				if (!string.IsNullOrWhiteSpace(apiResponse.Error))
				{
					_logger.LogWarning($"[ChatbotService] RAG API returned error: {apiResponse.Error}");
                   await SaveConversationAsync(userId, question, apiResponse.Error, ChatMessageStatus.Error, apiResponse.Error);
					return new ChatbotResponseDto
					{
						Success = false,
						Message = apiResponse.Error,
						Answer = null
					};
				}

				var finalAnswer = string.IsNullOrWhiteSpace(apiResponse.Answer)
					? "Mình chưa tìm thấy thông tin phù hợp trong dữ liệu hiện tại. Bạn thử hỏi cụ thể hơn về tên sự kiện, thời gian hoặc địa điểm nhé."
					: apiResponse.Answer;

				_logger.LogInformation($"[ChatbotService] Final answer: '{finalAnswer.Substring(0, Math.Min(100, finalAnswer.Length))}...'");
				await SaveConversationAsync(userId, question, finalAnswer, ChatMessageStatus.Final);

				return new ChatbotResponseDto
				{
					Success = true,
					Message = "Thành công",
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
				await SaveConversationAsync(userId, question, $"Lỗi: {ex.Message}", ChatMessageStatus.Error, ex.Message);
				return new ChatbotResponseDto
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Answer = null
				};
			}
		}

		/// <summary>
		/// Kiểm tra trạng thái RAG API server
		/// </summary>
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

		private async Task SaveConversationAsync(string userId, string question, string answer, ChatMessageStatus assistantStatus, string? errorMessage = null)
		{
			try
			{
               var userRole = GetCurrentUserRole();

				var session = await _unitOfWork.ChatbotSessions.GetAsync(
					s => s.UserId == userId && s.Status == ChatSessionStatus.Active,
					q => q.OrderByDescending(s => s.StartedAt));

				if (session == null)
				{
					session = new ChatbotSession
					{
						UserId = userId,
						StartedAt = DateTime.UtcNow,
						Status = ChatSessionStatus.Active
					};

					await _unitOfWork.ChatbotSessions.CreateAsync(session);
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
