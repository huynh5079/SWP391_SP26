using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System.Threading.Tasks;

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
            var log = new UserActivityLog
            {
                UserId = userId,
                ActionType = actionType,
                TargetId = targetId,
                TargetType = targetType,
                Description = description
            };

            await _unitOfWork.UserActivityLogs.CreateAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
