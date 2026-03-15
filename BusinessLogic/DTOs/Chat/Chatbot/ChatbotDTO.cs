using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Service;
using DataAccess.Enum;

namespace BusinessLogic.DTOs.Chat.Chatbot
{
	public class ChatbotResponseDto
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public string? Answer { get; set; }
		public string? SessionId { get; set; }
		public RoleEnum? Role { get; set; }
		public SourceDto[] Sources { get; set; } = Array.Empty<SourceDto>();
	}

	public class SourceDto
	{
		public float Score { get; set; }
		public Dictionary<string, object>? Meta { get; set; }
	}

	public class HealthStatusDto
	{
		public bool IsHealthy { get; set; }
		public string? Status { get; set; }
		public string? Message { get; set; }
	}

	// Internal DTO for RAG API response
	internal class RagApiResponse
	{
		public string? SessionId { get; set; }
		public string? Answer { get; set; }
		public RagApiSource[]? Sources { get; set; }
		public string? Error { get; set; }
	}

	internal class RagApiSource
	{
		public string? Score { get; set; }
		public Dictionary<string, object>? Meta { get; set; }
	}
	public class AskChatbotRequest
	{
		public string? Question { get; set; }
		public int? TopK { get; set; } = 5;
		public string? SessionId { get; set; }
	}

	public class ChatbotHistoryMessageDto
	{
		public string Sender { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}

	public class ChatbotConversationHistoryDto
	{
		public string? SessionId { get; set; }
		public List<ChatbotHistoryMessageDto> Messages { get; set; } = new();
	}
}
