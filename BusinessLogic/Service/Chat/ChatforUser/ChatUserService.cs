using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.DTOs.Chat;
using BusinessLogic.Service.Chat.ChatforUser.ChatPerMission;
using DataAccess.Repositories.Abstraction;

namespace BusinessLogic.Service.Chat.ChatforUser
{
	public class ChatUserService : IChatUserService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IChatPermissionService _chatPermissionService;

		public ChatUserService(IUnitOfWork unitOfWork, IChatPermissionService chatPermissionService)
		{
			_unitOfWork = unitOfWork;
			_chatPermissionService = chatPermissionService;
		}

		public async Task<IReadOnlyList<ChatContactDto>> GetContactsAsync(string currentUserId, string currentRole)
		{
			var allowedRoles = _chatPermissionService.GetAllowedRoles(currentRole);
			var users = await _unitOfWork.ChatRepository.GetChatUsersAsync(currentUserId, allowedRoles);
			var summaries = await _unitOfWork.ChatRepository.GetConversationSummariesAsync(currentUserId);

			return users
				.Select(user =>
				{
					summaries.TryGetValue(user.Id, out var summary);
					return new ChatContactDto
					{
						UserId = user.Id,
						FullName = user.FullName,
						RoleName = user.Role.RoleName?.ToString() ?? string.Empty,
						AvatarUrl = user.AvatarUrl,
						LastMessage = summary.LastMessage,
						LastMessageAt = summary.LastMessageAt,
						UnreadCount = summary.UnreadCount
					};
				})
				.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue)
				.ThenBy(x => x.FullName)
				.ToList();
		}

		public async Task<IReadOnlyList<ChatMessageDto>> GetConversationAsync(string currentUserId, string currentRole, string otherUserId)
		{
			var otherUser = await _unitOfWork.ChatRepository.GetUserWithRoleAsync(otherUserId);
			if (otherUser == null)
			{
				throw new KeyNotFoundException("Không tìm thấy người dùng chat.");
			}

			var otherRole = otherUser.Role.RoleName?.ToString() ?? string.Empty;
			if (!_chatPermissionService.CanChat(currentRole, otherRole))
			{
				throw new UnauthorizedAccessException("Role hiện tại không được chat với người dùng này.");
			}

			await _unitOfWork.ChatRepository.MarkConversationReadAsync(currentUserId, otherUserId);
			var messages = await _unitOfWork.ChatRepository.GetConversationMessagesAsync(currentUserId, otherUserId);

			return messages
				.Select(message => new ChatMessageDto
				{
					SenderId = ExtractSenderId(message.ErrorMessage),
					ReceiverId = ExtractReceiverId(message.ErrorMessage),
					Content = message.Content,
					SentAt = message.CreatedAt
				})
				.ToList();
		}

		public async Task<ChatMessageDto> SendPrivateMessageAsync(string senderUserId, string senderRole, string receiverUserId, string content)
		{
			if (string.IsNullOrWhiteSpace(senderUserId))
			{
				throw new UnauthorizedAccessException("Không xác định được người gửi.");
			}

			if (string.IsNullOrWhiteSpace(receiverUserId) || string.IsNullOrWhiteSpace(content))
			{
				throw new ArgumentException("Thiếu thông tin tin nhắn.");
			}

			if (senderUserId == receiverUserId)
			{
				throw new InvalidOperationException("Không thể gửi tin nhắn cho chính mình.");
			}

			var receiver = await _unitOfWork.ChatRepository.GetUserWithRoleAsync(receiverUserId);
			if (receiver == null)
			{
				throw new KeyNotFoundException("Người nhận không tồn tại hoặc không khả dụng.");
			}

			var receiverRole = receiver.Role.RoleName?.ToString() ?? string.Empty;
			if (!_chatPermissionService.CanChat(senderRole, receiverRole))
			{
				throw new UnauthorizedAccessException("Role hiện tại không được chat với người dùng này.");
			}

			var message = await _unitOfWork.ChatRepository.AddDirectMessageAsync(senderUserId, receiverUserId, content);

			return new ChatMessageDto
			{
				SenderId = ExtractSenderId(message.ErrorMessage),
				ReceiverId = ExtractReceiverId(message.ErrorMessage),
				Content = message.Content,
				SentAt = message.CreatedAt
			};
		}

		public async Task MarkConversationReadAsync(string userId, string otherUserId)
		{
			await _unitOfWork.ChatRepository.MarkConversationReadAsync(userId, otherUserId);
		}

		private static string ExtractSenderId(string? metadata)
		{
			if (string.IsNullOrWhiteSpace(metadata))
			{
				return string.Empty;
			}

			foreach (var part in metadata.Split(';', StringSplitOptions.RemoveEmptyEntries))
			{
				if (part.StartsWith("sender:", StringComparison.OrdinalIgnoreCase))
				{
					return part.Substring("sender:".Length);
				}
			}

			return string.Empty;
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
	}
}
