using System.Threading.Tasks;
using BusinessLogic.Service.System;

namespace AEMS_WPF.Services
{
    public class DummySignalRNotifier : ISignalRNotifier
    {
        public Task SendNotificationToUserAsync(string userId, string title, string message)
        {
            // In WPF client, we don't host the SignalR Hub.
            // Notifications are typically fetched from the database or via a SignalR Client.
            return Task.CompletedTask;
        }

        public Task SendCheckInNotificationAsync(string eventId, string fullName, string message, string? avatarUrl)
        {
            return Task.CompletedTask;
        }
    }
}
