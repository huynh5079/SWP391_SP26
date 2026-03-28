using System;
using System.Threading.Tasks;

namespace BusinessLogic.Service.System
{
    public interface INotificationService
    {


        /// <summary>
        /// Sends a generic notification using a typed request object.
        /// </summary>
        /// <param name="request">The notification request detail.</param>
        Task SendNotificationAsync(BusinessLogic.DTOs.SendNotificationRequest request);
        Task<IEnumerable<DataAccess.Entities.Notification>> GetUserNotificationsAsync(string userId);
        Task DeleteNotificationAsync(string notificationId);
        Task ClearAllNotificationsAsync(string userId);
    }
}
