using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Enum;
using DataAccess.Entities;

namespace BusinessLogic.Service.UserActivities
{
    public interface IUserActivityLogService
    {
        Task LogActivityAsync(string userId, UserActionType actionType, string? targetId = null, TargetType targetType = TargetType.None, string? description = null);
        Task<List<UserActivityLog>> GetRecentActivitiesAsync(string userId, int count = 20);
        Task<string> GetUserPersonalizationContextAsync(string userId);
    }
}
