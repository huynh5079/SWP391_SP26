using System;
using System.Threading.Tasks;

namespace BusinessLogic.Service.System
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a generic notification to a user.
        /// </summary>
        /// <param name="userId">The User ID receiving the notification.</param>
        /// <param name="title">Title of the notification.</param>
        /// <param name="message">Body/message of the notification.</param>
        /// <param name="type">Type of notification (e.g., "StudentRegistration", "EventActivity", "SystemAlert"). Used to categorize or build Activity Logs later.</param>
        Task SendNotificationAsync(string userId, string title, string message, string type);
    }
}
