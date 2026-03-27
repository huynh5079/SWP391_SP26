using System.Threading.Tasks;

namespace BusinessLogic.Service.ActivityLog
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string userId, string actionType, string? targetId, string? targetType, string description);
    }
}
