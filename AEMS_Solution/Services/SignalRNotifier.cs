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
    }
}
