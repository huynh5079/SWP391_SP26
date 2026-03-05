using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Helper;
using DataAccess.Repositories.Abstraction;
using BusinessLogic.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Service.System
{
    public class SystemErrorLogService : ISystemErrorLogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SystemErrorLogService(IServiceProvider serviceProvider, IHubContext<NotificationHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        public async Task LogErrorAsync(Exception ex, string? userId, string source, DataAccess.Enum.SystemLogStatusEnum? statusCode = DataAccess.Enum.SystemLogStatusEnum.ServerError)
        {
            try
            {
                // Use a fresh DI scope to avoid reusing a "poisoned" DbContext from a failed request
                using var scope = _serviceProvider.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Drill down to the innermost exception for the most accurate message
                var deepestEx = ex;
                while (deepestEx.InnerException != null)
                    deepestEx = deepestEx.InnerException;

                var formattedMessage = $"{ex.Message} | Root Cause: {deepestEx.Message}";

                var errorLog = new SystemErrorLog
                {
                    ExceptionType    = deepestEx.GetType().Name,
                    ExceptionMessage = formattedMessage,
                    StackTrace       = deepestEx.StackTrace ?? ex.StackTrace,
                    Source           = source,
                    UserId           = userId,
                    StatusCode       = (int?)statusCode
                };

                var now = DateTimeHelper.GetVietnamTime();
                errorLog.CreatedAt = now;
                errorLog.UpdatedAt = now;

                await uow.SystemErrorLogs.CreateAsync(errorLog);
                await uow.SaveChangesAsync();

                // ── Push real-time notification to all Admin users ─────────────────────
                // Save to DB first (via notification entity), then push via SignalR group
                await NotifyAdminsAsync(uow, deepestEx.GetType().Name, source, formattedMessage, errorLog.Id);
            }
            catch (Exception logException)
            {
                // Last-resort fallback: print to console so it's never silently swallowed
                Console.WriteLine($"[SystemErrorLogService] Failed to log error: {logException.Message}");
            }
        }

        // ─── Push in-app notifications to every Admin in the system ────────────────
        private async Task NotifyAdminsAsync(IUnitOfWork uow, string exceptionTypeName, string source, string shortMessage, string? errorLogId)
        {
            try
            {
                // Fetch all active Admin accounts — compare with enum value, not string
                var admins = await uow.Users.GetAllAsync(u =>
                    u.Role != null &&
                    u.Role.RoleName == RoleEnum.Admin &&
                    u.DeletedAt == null);

                string title   = $"[System Error] {exceptionTypeName}";
                string message = $"Source: {source} — {shortMessage.Truncate(180)}";

                foreach (var admin in admins)
                {
                    // 1. Persist notification to DB
                    var notification = new Notification
                    {
                        UserId  = admin.Id,
                        Title   = title,
                        Message = message,
                        Type    = "SystemError",
                        IsRead  = false
                    };
                    var ts = DateTimeHelper.GetVietnamTime();
                    notification.CreatedAt = ts;
                    notification.UpdatedAt = ts;

                    await uow.Notifications.CreateAsync(notification);

                    // 2. Fire real-time SignalR push to the admin's group
                    await _hubContext.Clients.Group(admin.Id).SendAsync("ReceiveNotification", title, message);
                }

                await uow.SaveChangesAsync();
            }
            catch (Exception notifyEx)
            {
                // Notification failure must never break the main error logging flow
                Console.WriteLine($"[SystemErrorLogService] Failed to notify admins: {notifyEx.Message}");
            }
        }

        public async Task<PaginationResult<SystemErrorLog>> GetLogsAsync(int page, int pageSize, string? search, DataAccess.Enum.SystemLogStatusEnum? statusCode)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var query = await uow.SystemErrorLogs.GetAllAsync();
            IEnumerable<SystemErrorLog> result = query;

            if (statusCode.HasValue)
            {
                var codeVal = (int)statusCode.Value;
                result = result.Where(x => x.StatusCode == codeVal);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                result = result.Where(x =>
                    (x.ExceptionMessage != null && x.ExceptionMessage.ToLower().Contains(lowerSearch)) ||
                    (x.Source           != null && x.Source.ToLower().Contains(lowerSearch))           ||
                    (x.UserId           != null && x.UserId.ToLower().Contains(lowerSearch))
                );
            }

            result = result.OrderByDescending(x => x.CreatedAt);

            var totalCount = result.Count();
            var data       = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginationResult<SystemErrorLog>(data, totalCount, page, pageSize);
        }

        public async Task DeleteOldLogsAsync(int days = 30)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var threshold = DateTimeHelper.GetVietnamTime().AddDays(-days);
            var oldLogs   = await uow.SystemErrorLogs.GetAllAsync(x => x.CreatedAt < threshold);

            if (oldLogs != null && oldLogs.Any())
            {
                foreach (var log in oldLogs)
                    await uow.SystemErrorLogs.RemoveAsync(log);

                await uow.SaveChangesAsync();
            }
        }

        public async Task<(List<string> Dates, List<int> Counts, int TodayCount)> GetErrorTrendAsync(int days = 7)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var threshold    = DateTimeHelper.GetVietnamTime().Date.AddDays(-(days - 1));
            var logs         = await uow.SystemErrorLogs.GetAllAsync(x => x.CreatedAt >= threshold);
            var vietnamToday = DateTimeHelper.GetVietnamTime().Date;
            int todayCount   = logs.Count(x => x.CreatedAt.Date == vietnamToday);

            var dates  = new List<string>();
            var counts = new List<int>();

            for (int i = 0; i < days; i++)
            {
                var date  = threshold.AddDays(i);
                dates.Add(date.ToString("dd/MM"));
                counts.Add(logs.Count(x => x.CreatedAt.Date == date));
            }

            return (dates, counts, todayCount);
        }
    }

    // ─── String extension: truncate long messages ──────────────────────────────
    internal static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
