using BusinessLogic.Helper;
using BusinessLogic.Service.Interface;
using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Service
{
    public class SystemErrorLogService : ISystemErrorLogService
    {
        private readonly IServiceProvider _serviceProvider;

        public SystemErrorLogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LogErrorAsync(Exception ex, string? userId, string source)
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
                        StatusCode = null
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
    }
}
