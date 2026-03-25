using System.Security.Claims;
using BusinessLogic.DTOs;
using BusinessLogic.Service.Chat.ChatforUser;
using BusinessLogic.Service.System;
using DataAccess.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BusinessLogic.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatUserService _chatUserService;
        private readonly IChatPresenceTracker _presenceTracker;
        private readonly INotificationService _notificationService;
        public ChatHub(IChatUserService chatUserService, IChatPresenceTracker presenceTracker, INotificationService notificationService)
        {
            _chatUserService = chatUserService;
            _presenceTracker = presenceTracker;
            _notificationService = notificationService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _presenceTracker.UserConnected(userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _presenceTracker.UserDisconnected(userId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string receiverUserId, string content)
        {
            var senderUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var senderRole = Context.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(senderUserId))
            {
                throw new HubException("Không xác định được người gửi.");
            }

            if (string.IsNullOrWhiteSpace(receiverUserId) || string.IsNullOrWhiteSpace(content))
            {
                throw new HubException("Thiếu thông tin tin nhắn.");
            }

            if (senderUserId == receiverUserId)
            {
                throw new HubException("Không thể gửi tin nhắn cho chính mình.");
            }

            try
            {
                var message = await _chatUserService.SendPrivateMessageAsync(senderUserId, senderRole, receiverUserId, content);

                await Clients.Group(senderUserId).SendAsync("ReceivePrivateMessage", message);
                await Clients.Group(receiverUserId).SendAsync("ReceivePrivateMessage", message);

                // Gửi thông báo lên chuông cho người nhận
                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    ReceiverId = receiverUserId,
                    Title = "Tin nhắn mới",
                    Message = content.Length > 60 ? content[..60] + "..." : content,
                    Type = NotificationType.NewChatMessage,
                    RelatedEntityId = senderUserId
                });
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is ArgumentException)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task RecallPrivateMessage(string messageId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Không xác định được người dùng.");
            }

            try
            {
                var message = await _chatUserService.RecallMessageAsync(userId, messageId);
                await Clients.Group(message.SenderId).SendAsync("MessageRecalled", message);
                await Clients.Group(message.ReceiverId).SendAsync("MessageRecalled", message);
            }
            catch (Exception ex) when (ex is KeyNotFoundException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is ArgumentException)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task MarkConversationRead(string otherUserId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(otherUserId))
            {
                await _chatUserService.MarkConversationReadAsync(userId, otherUserId);
                await Clients.Group(otherUserId).SendAsync("ConversationRead", new
                {
                    ReaderUserId = userId,
                    OtherUserId = otherUserId
                });
            }
        }
    }
}
