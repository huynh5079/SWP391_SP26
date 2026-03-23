using BusinessLogic.Service.System;
using Microsoft.AspNetCore.SignalR;
using AEMS_Solution.Hubs;
using System.Threading.Tasks;

namespace AEMS_Solution.Services
{
    public class SignalRNotifier : ISignalRNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotifier(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(string userId, string title, string message)
        {
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
        }

        public async Task SendCheckInNotificationAsync(string eventId, string fullName, string message)
        {
            // Broadcast to the specific event group
            await _hubContext.Clients.Group($"Event_{eventId}").SendAsync("ReceiveCheckIn", eventId, fullName, message);
        }
    }
}
