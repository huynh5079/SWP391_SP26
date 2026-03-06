using System.Threading.Tasks;

namespace BusinessLogic.Service.System
{
    public interface ISignalRNotifier
    {
        Task SendNotificationToUserAsync(string userId, string title, string message);
    }
}
