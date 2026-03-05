using BusinessLogic.Hubs;
using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Service.System
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISystemErrorLogService _errorLogService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IUnitOfWork uow, ISystemErrorLogService errorLogService, IHubContext<NotificationHub> hubContext)
        {
            _uow = uow;
            _errorLogService = errorLogService;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string type)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false
                };

                await _uow.Notifications.CreateAsync(notification);
                await _uow.SaveChangesAsync();

                // 2. Fire Real-Time SignalR Event
                await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
            }
            catch (Exception ex)
            {
                // We log the error but don't throw it. A failed notification should not break the main business flow.
                await _errorLogService.LogErrorAsync(ex, userId, $"NotificationService.SendNotificationAsync (Type: {type})");
            }
        }
    }
}
