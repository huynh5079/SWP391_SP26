using AEMS_Solution.Extensions;
using BusinessLogic.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AEMS_Solution.Middlewares
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GlobalExceptionHandler(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandler> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred.");

                // Create a scope to resolve scoped services (since Middleware is Singleton)
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        var errorLogService = scope.ServiceProvider.GetRequiredService<ISystemErrorLogService>();
                        
                        // Extract userId from ClaimsPrincipal
                        string? userId = null;
                        if (context.User?.Identity?.IsAuthenticated == true)
                        {
                            userId = context.User.GetUserId();
                        }

                        // Determine source (controller/action or endpoint)
                        string source = context.Request.Path.HasValue 
                            ? context.Request.Path.Value 
                            : "Unknown";

                        // Log error to database
                        await errorLogService.LogErrorAsync(ex, userId, source, DataAccess.Enum.SystemLogStatusEnum.ServerError);
                    }
                    catch (Exception logException)
                    {
                        // If logging fails, at least log to console/logger
                        _logger.LogError(logException, "Failed to log error to database.");
                    }
                }

                // Redirect user to error page (don't show technical exception)
                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }
}
