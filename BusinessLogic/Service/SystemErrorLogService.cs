using BusinessLogic.Helper;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Threading.Tasks;

namespace BusinessLogic.Service
{
    public class SystemErrorLogService : ISystemErrorLogService
    {
        private readonly IUnitOfWork _uow;

        public SystemErrorLogService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task LogErrorAsync(Exception ex, string? userId, string source)
        {
            try
            {
                var errorLog = new SystemErrorLog
                {
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    Source = source,
                    UserId = userId,
                    StatusCode = null // Can be set from HttpContext if available
                };

                // Timestamps are automatically set by BaseEntity constructor
                // But we ensure they use Vietnam time
                var vietnamTime = DateTimeHelper.GetVietnamTime();
                errorLog.CreatedAt = vietnamTime;
                errorLog.UpdatedAt = vietnamTime;

                await _uow.SystemErrorLogs.CreateAsync(errorLog);
                await _uow.SaveChangesAsync();
            }
            catch (Exception logException)
            {
                // Self-correction: If logging itself fails, only console log
                // This ensures the logging process doesn't crash the app
                Console.WriteLine($"[SystemErrorLogService] Failed to log error: {logException.Message}");
                Console.WriteLine($"[SystemErrorLogService] Original error: {ex.Message}");
            }
        }
    }
}
