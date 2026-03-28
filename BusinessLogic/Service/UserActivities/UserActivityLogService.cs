using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccess.Repositories.Abstraction;
using DataAccess.Enum;
using DataAccess.Entities;
using System.Collections.Generic;

namespace BusinessLogic.Service.UserActivities
{
    public class UserActivityLogService : IUserActivityLogService
    {
        private readonly IUnitOfWork _uow;

        public UserActivityLogService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task LogActivityAsync(string userId, UserActionType actionType, string? targetId = null, TargetType targetType = TargetType.None, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || userId == "anonymous") return;

            var log = new UserActivityLog
            {
                UserId = userId,
                ActionType = actionType,
                TargetId = targetId,
                TargetType = targetType,
                Description = description
            };

            await _uow.UserActivityLogs.CreateAsync(log);
            await _uow.SaveChangesAsync();
        }

        public async Task<List<UserActivityLog>> GetRecentActivitiesAsync(string userId, int count = 20)
        {
            return (await _uow.UserActivityLogs.GetAllAsync(
                x => x.UserId == userId,
                q => q.OrderByDescending(x => x.CreatedAt).Take(count)
            )).ToList();
        }

        public async Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            return await _uow.UserActivityLogs.GetAllAsync(
                null,
                q => q.Include(l => l.User).OrderByDescending(l => l.CreatedAt).Take(count)
            );
        }

        public async Task<DataAccess.Entities.PaginationResult<UserActivityLog>> GetLogsAsync(int page, int pageSize, string? search)
        {
            var logs = await _uow.UserActivityLogs.GetAllAsync(null, q => q.Include(l => l.User));
            IEnumerable<UserActivityLog> result = logs;

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                result = result.Where(x =>
                    (x.Description != null && x.Description.ToLower().Contains(lowerSearch)) ||
                    (x.User != null && x.User.FullName != null && x.User.FullName.ToLower().Contains(lowerSearch))
                );
            }

            result = result.OrderByDescending(x => x.CreatedAt);
            var totalCount = result.Count();
            var data = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new DataAccess.Entities.PaginationResult<UserActivityLog>(data, totalCount, page, pageSize);
        }

        public async Task DeleteOldLogsAsync(int days = 30)
        {
            var threshold = DataAccess.Helper.DateTimeHelper.GetVietnamTime().AddDays(-days);
            var oldLogs = await _uow.UserActivityLogs.GetAllAsync(x => x.CreatedAt < threshold);

            if (oldLogs != null && oldLogs.Any())
            {
                foreach (var log in oldLogs)
                    await _uow.UserActivityLogs.RemoveAsync(log);

                await _uow.SaveChangesAsync();
            }
        }

        public async Task<string> GetUserPersonalizationContextAsync(string userId)
        {
            var logs = await GetRecentActivitiesAsync(userId, 20);
            if (!logs.Any()) return string.Empty;

            var contextLines = new List<string>
            {
                "[NGỮ CẢNH CÁ NHÂN HOÁ - từ lịch sử tương tác của người dùng]"
            };

            // 1. Sự kiện được tương tác gần nhất (để hiểu đại từ "sự kiện đó", "event này")
            var lastEventLog = logs.FirstOrDefault(l => l.TargetType == TargetType.Event && !string.IsNullOrWhiteSpace(l.TargetId));
            if (lastEventLog != null)
            {
                var eventObj = await _uow.Events.GetAsync(e => e.Id == lastEventLog.TargetId);
                if (eventObj != null)
                {
                    contextLines.Add($"- Sự kiện được tương tác gần nhất: \"{eventObj.Title}\" (nếu người dùng nói 'sự kiện đó', 'cái đó', 'nó'... thì ngầm hiểu là sự kiện này)");
                }
            }

            // 2. Các câu hỏi thực tế user đã hỏi chatbot gần đây (lấy tối đa 5 câu gần nhất)
            var recentChatLogs = logs
                .Where(l => l.ActionType == UserActionType.USER_ASKED_CHATBOT && !string.IsNullOrWhiteSpace(l.Description))
                .Take(5)
                .Select(l => $"  • \"{l.Description}\"")
                .ToList();
            if (recentChatLogs.Any())
            {
                contextLines.Add($"- Các câu hỏi gần đây của người dùng:");
                contextLines.AddRange(recentChatLogs);
                contextLines.Add("  → Dựa vào lịch sử trên để đoán context nếu câu hỏi mới không rõ ràng.");
            }

            // 3. Sự kiện user đã xem/đăng ký (topic sở thích)
            var viewedEvents = logs
                .Where(l => (l.ActionType == UserActionType.USER_VIEWED_EVENT || l.ActionType == UserActionType.USER_REGISTERED_EVENT)
                             && !string.IsNullOrWhiteSpace(l.Description))
                .Select(l => l.Description!)
                .Distinct()
                .Take(4)
                .ToList();
            if (viewedEvents.Any())
            {
                contextLines.Add($"- Sự kiện user đã xem/đăng ký gần đây: {string.Join(", ", viewedEvents.Select(e => $"\"{e}\""))}");
                contextLines.Add("  → Ưu tiên gợi ý sự kiện tương tự những sự kiện trên khi user hỏi chung chung.");
            }

            // Chỉ trả context nếu có ít nhất 2 dòng thực sự (ngoài header)
            if (contextLines.Count <= 1) return string.Empty;
            return string.Join("\n", contextLines);
        }
    }
}
