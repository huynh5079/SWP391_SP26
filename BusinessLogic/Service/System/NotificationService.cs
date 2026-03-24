using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
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
    }
}    