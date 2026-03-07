using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
	public class ChatRepository : IChatRepository
	{
		private readonly AEMSContext _context;
		private const string DirectMessagePrefix = "DM::";

		public ChatRepository(AEMSContext context)
		{
			_context = context;
		}

		public async Task<User?> GetUserWithRoleAsync(string userId)
		{
			return await _context.Users
				.AsNoTracking()
				.Include(x => x.Role)
				.FirstOrDefaultAsync(x => x.Id == userId && x.DeletedAt == null && x.IsBanned != true);
		}

		public async Task<List<User>> GetChatUsersAsync(string currentUserId, IEnumerable<string> allowedRoles)
		{
			var allowedRoleSet = allowedRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);

			return await _context.Users
				.AsNoTracking()
				.Include(x => x.Role)
				.Where(x => x.Id != currentUserId
					&& x.DeletedAt == null
					&& x.IsBanned != true
					&& x.Role.RoleName != null
					&& allowedRoleSet.Contains(x.Role.RoleName.ToString()!))
				.OrderBy(x => x.FullName)
				.ToListAsync();
		}

		public async Task<ChatSession?> FindDirectSessionAsync(string firstUserId, string secondUserId)
		{
			var pairKey = GetConversationKey(firstUserId, secondUserId);

			return await _context.ChatSessions
				.FirstOrDefaultAsync(x => !x.IsDeleted && x.Title == pairKey);
		}

		public async Task<ChatSession> GetOrCreateDirectSessionAsync(string firstUserId, string secondUserId)
		{
			var existingSession = await FindDirectSessionAsync(firstUserId, secondUserId);
			if (existingSession != null)
			{
				return existingSession;
			}

			var orderedParticipants = OrderParticipants(firstUserId, secondUserId);
			var session = new ChatSession
			{
				UserId = orderedParticipants[0],
				Title = GetConversationKey(firstUserId, secondUserId),
				Status = ChatSessionStatus.Active,
				IsDeleted = false
			};

			_context.ChatSessions.Add(session);
			await _context.SaveChangesAsync();
			return session;
		}

		public async Task<ChatMessage> AddDirectMessageAsync(string senderId, string receiverId, string content)
		{
			var session = await GetOrCreateDirectSessionAsync(senderId, receiverId);
			var message = new ChatMessage
			{
				SessionId = session.Id,
				Sender = "user",
				Content = content.Trim(),
				ReplyToMessageId = senderId,
				ErrorMessage = BuildMetadata(receiverId, new[] { senderId }),
				Status = ChatMessageStatus.Final,
				IsDeleted = false
			};

			_context.ChatMessages.Add(message);
			await _context.SaveChangesAsync();
			return message;
		}

		public async Task<List<ChatMessage>> GetConversationMessagesAsync(string firstUserId, string secondUserId)
		{
			var session = await FindDirectSessionAsync(firstUserId, secondUserId);
			if (session == null)
			{
				return new List<ChatMessage>();
			}

			return await _context.ChatMessages
				.AsNoTracking()
				.Where(x => x.SessionId == session.Id && !x.IsDeleted)
				.OrderBy(x => x.CreatedAt)
				.ToListAsync();
		}

		public async Task<Dictionary<string, (string? LastMessage, DateTime? LastMessageAt, int UnreadCount)>> GetConversationSummariesAsync(string userId)
		{
			var result = new Dictionary<string, (string? LastMessage, DateTime? LastMessageAt, int UnreadCount)>();

			var sessions = await _context.ChatSessions
				.AsNoTracking()
				.Where(x => !x.IsDeleted && x.Title != null && x.Title.StartsWith(DirectMessagePrefix))
				.ToListAsync();

			foreach (var session in sessions)
			{
				var participants = ParseParticipants(session.Title);
				if (!participants.Contains(userId))
				{
					continue;
				}

				var otherUserId = participants.FirstOrDefault(x => x != userId);
				if (string.IsNullOrWhiteSpace(otherUserId))
				{
					continue;
				}

				var messages = await _context.ChatMessages
					.AsNoTracking()
					.Where(x => x.SessionId == session.Id && !x.IsDeleted)
					.OrderBy(x => x.CreatedAt)
					.ToListAsync();

				var lastMessage = messages.LastOrDefault();
				var unreadCount = messages.Count(x => (x.ReplyToMessageId ?? string.Empty) != userId && !HasBeenReadBy(x.ErrorMessage, userId));
				result[otherUserId] = (lastMessage?.Content, lastMessage?.CreatedAt, unreadCount);
			}

			return result;
		}

		public async Task MarkConversationReadAsync(string userId, string otherUserId)
		{
			var session = await FindDirectSessionAsync(userId, otherUserId);
			if (session == null)
			{
				return;
			}

			var messages = await _context.ChatMessages
				.Where(x => x.SessionId == session.Id && !x.IsDeleted && (x.ReplyToMessageId ?? string.Empty) != userId)
				.ToListAsync();

			var changed = false;
			foreach (var message in messages)
			{
				if (HasBeenReadBy(message.ErrorMessage, userId))
				{
					continue;
				}

				var receiverId = ExtractReceiverId(message.ErrorMessage);
				var readBy = ExtractReadBy(message.ErrorMessage);
				readBy.Add(userId);
				message.ErrorMessage = BuildMetadata(receiverId, readBy);
				changed = true;
			}

			if (changed)
			{
				await _context.SaveChangesAsync();
			}
		}

		private static string GetConversationKey(string firstUserId, string secondUserId)
		{
			var orderedParticipants = OrderParticipants(firstUserId, secondUserId);
			return $"{DirectMessagePrefix}{orderedParticipants[0]}::{orderedParticipants[1]}";
		}

		private static string[] OrderParticipants(string firstUserId, string secondUserId)
		{
			return string.CompareOrdinal(firstUserId, secondUserId) <= 0
				? new[] { firstUserId, secondUserId }
				: new[] { secondUserId, firstUserId };
		}

		private static string[] ParseParticipants(string? title)
		{
			if (string.IsNullOrWhiteSpace(title) || !title.StartsWith(DirectMessagePrefix))
			{
				return Array.Empty<string>();
			}

			var parts = title.Substring(DirectMessagePrefix.Length)
				.Split("::", StringSplitOptions.RemoveEmptyEntries);

			return parts.Length == 2 ? parts : Array.Empty<string>();
		}

		private static string BuildMetadata(string receiverId, IEnumerable<string> readBy)
		{
			return $"receiver:{receiverId};readBy:{string.Join(',', readBy.Distinct())}";
		}

		private static string ExtractReceiverId(string? metadata)
		{
			if (string.IsNullOrWhiteSpace(metadata))
			{
				return string.Empty;
			}

			foreach (var part in metadata.Split(';', StringSplitOptions.RemoveEmptyEntries))
			{
				if (part.StartsWith("receiver:", StringComparison.OrdinalIgnoreCase))
				{
					return part.Substring("receiver:".Length);
				}
			}

			return string.Empty;
		}

		private static HashSet<string> ExtractReadBy(string? metadata)
		{
			if (string.IsNullOrWhiteSpace(metadata))
			{
				return new HashSet<string>();
			}

			foreach (var part in metadata.Split(';', StringSplitOptions.RemoveEmptyEntries))
			{
				if (!part.StartsWith("readBy:", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				return part.Substring("readBy:".Length)
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.ToHashSet();
			}

			return new HashSet<string>();
		}

		private static bool HasBeenReadBy(string? metadata, string userId)
		{
			return ExtractReadBy(metadata).Contains(userId);
		}
	}
}
