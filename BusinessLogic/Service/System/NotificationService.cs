using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Service.System
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISystemErrorLogService _errorLogService;
        private readonly ISignalRNotifier _signalRNotifier;

        public NotificationService(IUnitOfWork uow, ISystemErrorLogService errorLogService, ISignalRNotifier signalRNotifier)
        {
            _uow = uow;
            _errorLogService = errorLogService;
            _signalRNotifier = signalRNotifier;
        }

        public async Task SendNotificationAsync(BusinessLogic.DTOs.SendNotificationRequest request)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = request.ReceiverId,
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type.ToString(),
                    RelatedEntityId = request.RelatedEntityId,
                    IsRead = false
                };

                await _uow.Notifications.CreateAsync(notification);
                await _uow.SaveChangesAsync();

                // Fire Real-Time SignalR Event
                await _signalRNotifier.SendNotificationToUserAsync(request.ReceiverId, request.Title, request.Message);
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(ex, request.ReceiverId, $"NotificationService.SendNotificationAsync (Type: {request.Type})");
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
            => await _uow.Notifications.GetAllAsync(n => n.UserId == userId);

        public async Task DeleteNotificationAsync(string notificationId)
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notification != null)
            {
                await _uow.Notifications.RemoveAsync(notification);
                await _uow.SaveChangesAsync();
            }
        }

        public async Task ClearAllNotificationsAsync(string userId)
        {
            var notifications = await _uow.Notifications.GetAllAsync(n => n.UserId == userId);
            foreach (var n in notifications)
                await _uow.Notifications.RemoveAsync(n);

            await _uow.SaveChangesAsync();
        }
    }
}