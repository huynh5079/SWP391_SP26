using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using DataAccess.Enum;
using System.Threading.Tasks;
using System;

namespace BusinessLogic.Service.ActivityLog
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivityLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogActivityAsync(string userId, string actionType, string? targetId, string? targetType, string description)
        {
            // Convert string to enum safely for the new UserActivityLog structure
            var action = Enum.TryParse<UserActionType>(actionType, true, out var a) ? a : UserActionType.Unknown;
            
            TargetType? target = null;
            if (!string.IsNullOrWhiteSpace(targetType) && Enum.TryParse<TargetType>(targetType, true, out var t))
            {
                target = t;
            }

            var log = new UserActivityLog
            {
                UserId = userId,
                ActionType = action,
                TargetId = targetId,
                TargetType = target,
                Description = description
            };

            await _unitOfWork.UserActivityLogs.CreateAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
