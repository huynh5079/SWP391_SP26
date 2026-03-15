using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Chat.Chatbot;
using DataAccess.Enum;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Service.Chat
{
	public interface IChatbotService
	{
		Task<ChatbotResponseDto> AskQuestionAsync(string question, int topK = 5, string? sessionId = null);
       Task<ChatbotConversationHistoryDto> GetConversationHistoryAsync(string? sessionId = null, int limit = 100);
       Task<List<ChatbotSessionSummaryDto>> GetConversationSessionsAsync(int limit = 20);
       Task<string> StartNewConversationAsync(string? currentSessionId = null);
		Task<HealthStatusDto> CheckHealthAsync();
		
	}
}
