using BusinessLogic.Service.System;
using BusinessLogic.Service.Admin.Interface;
using BusinessLogic.Service.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Service.Admin
{
    public class UserLockExpirationService : BackgroundService, IUserLockExpirationService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UserLockExpirationService> _logger;

        public UserLockExpirationService(IServiceScopeFactory serviceScopeFactory, ILogger<UserLockExpirationService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessExpiredUserLocksAsync(stoppingToken);

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task<int> ProcessExpiredUserLocksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var unlockedCount = await userService.ReactivateExpiredUsersAsync();

                if (unlockedCount > 0)
                {
                    _logger.LogInformation("Automatically reactivated {UnlockedCount} expired user locks.", unlockedCount);
                }

                return unlockedCount;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to process expired user locks.");
                return 0;
            }
        }
    }
}
