using BusinessLogic.Helper;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DataAccess.Helper;

namespace BusinessLogic.Service
{
    public class SystemErrorLogService : ISystemErrorLogService
    {
        private readonly IServiceProvider _serviceProvider;

        public SystemErrorLogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LogErrorAsync(Exception ex, string? userId, string source, DataAccess.Enum.SystemLogStatusEnum? statusCode = DataAccess.Enum.SystemLogStatusEnum.ServerError)
        {
            try
            {
                // Create a NEW scope to get a FRESH UnitOfWork (and DbContext)
                // This ensures we don't reuse the "poisoned" DbContext from the failed request
                using (var scope = _serviceProvider.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // Drill down to the innermost exception
                    var deepestEx = ex;
                    while (deepestEx.InnerException != null)
                    {
                        deepestEx = deepestEx.InnerException;
                    }

                    // Format the message
                    var formattedMessage = $"{ex.Message} | Inner Cause: {deepestEx.Message}";

                    var errorLog = new SystemErrorLog
                    {
                        ExceptionType = deepestEx.GetType().Name,
                        ExceptionMessage = formattedMessage,
                        StackTrace = deepestEx.StackTrace ?? ex.StackTrace,
                        Source = source,
                        UserId = userId,
                        StatusCode = (int?)statusCode
                    };

                    var vietnamTime = DateTimeHelper.GetVietnamTime();
                    errorLog.CreatedAt = vietnamTime;
                    errorLog.UpdatedAt = vietnamTime;

                    await uow.SystemErrorLogs.CreateAsync(errorLog);
                    await uow.SaveChangesAsync();
                }
            }
            catch (Exception logException)
            {
                Console.WriteLine($"[SystemErrorLogService] Failed to log error: {logException.Message}");
            }
        }
        
        public async Task<PaginationResult<SystemErrorLog>> GetLogsAsync(int page, int pageSize, string? search, DataAccess.Enum.SystemLogStatusEnum? statusCode)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Query using GetAllAsync with (null) filter because we are doing complex filtering (string contains) in memory
            // GenericRepository's simple filter might not support complex OR logic easily if it expects single expression.
            // But if we can pass expression x => ..., it works.
            // However, "search" involves OR logic which is fine in Expression.
            // But for simplicity and safety against LINQ provider limitations (if any), let's stick to in-memory for now 
            // OR try to build the expression. Given it's IEnumerable return, the filter runs in DB?
            // Wait, GenericRepository returns Task<IEnumerable>. If it uses EF .ToListAsync(), then it runs in DB.
            // If GetAllAsync takes Expression<Func<T, bool>>, it runs in DB.
            // Let's stick to current logic for GetLogsAsync but uncomment it.
            
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
                    (x.Source != null && x.Source.ToLower().Contains(lowerSearch)) ||
                    (x.UserId != null && x.UserId.ToLower().Contains(lowerSearch))
                );
            }

            result = result.OrderByDescending(x => x.CreatedAt);

            var totalCount = result.Count();
            var data = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginationResult<SystemErrorLog>(data, totalCount, page, pageSize);
        }

        public async Task DeleteOldLogsAsync(int days = 30)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var threshold = DateTimeHelper.GetVietnamTime().AddDays(-days);
            
            // Use filter in GetAllAsync
            var oldLogs = await uow.SystemErrorLogs.GetAllAsync(x => x.CreatedAt < threshold);
            
            if (oldLogs != null && oldLogs.Any())
            {
                foreach (var log in oldLogs)
                {
                   await uow.SystemErrorLogs.RemoveAsync(log);
                }
                await uow.SaveChangesAsync();
            }
        }

        public async Task<(List<string> Dates, List<int> Counts, int TodayCount)> GetErrorTrendAsync(int days = 7)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var threshold = DateTimeHelper.GetVietnamTime().Date.AddDays(-(days - 1)); // Include today
            
            // Get logs from threshold
            // Note: In-memory grouping for MVP. Better to group in DB if possible.
            var logs = await uow.SystemErrorLogs.GetAllAsync(x => x.CreatedAt >= threshold);
            
            var vietnamToday = DateTimeHelper.GetVietnamTime().Date;
            var todayCount = logs.Count(x => x.CreatedAt.Date == vietnamToday);

            var dates = new List<string>();
            var counts = new List<int>();

            for (int i = 0; i < days; i++)
            {
                var date = threshold.AddDays(i);
                var label = date.ToString("dd/MM");
                var count = logs.Count(x => x.CreatedAt.Date == date);
                
                dates.Add(label);
                counts.Add(count);
            }

            return (dates, counts, todayCount);
        }
    }
}
