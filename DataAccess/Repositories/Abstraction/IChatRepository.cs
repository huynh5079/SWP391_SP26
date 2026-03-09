using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Entities;

namespace DataAccess.Repositories.Abstraction
{
	public interface IChatRepository
	{
        Task<User?> GetUserWithRoleAsync(string userId);
        Task<List<User>> GetChatUsersAsync(string currentUserId, IEnumerable<string> allowedRoles);
        Task<ChatSession?> FindDirectSessionAsync(string firstUserId, string secondUserId);
        Task<ChatSession> GetOrCreateDirectSessionAsync(string firstUserId, string secondUserId);
        Task<ChatMessage> AddDirectMessageAsync(string senderId, string receiverId, string content);
        Task<ChatMessage> RecallMessageAsync(string messageId, string userId);
        Task<List<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId);
        Task<Dictionary<string, (string? LastMessage, DateTime? LastMessageAt, int UnreadCount)>> GetConversationSummariesAsync(string userId);
        Task MarkConversationReadAsync(string userId, string otherUserId);
	}

}
