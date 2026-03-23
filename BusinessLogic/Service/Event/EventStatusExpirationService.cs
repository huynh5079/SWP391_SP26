using BusinessLogic.Service.System;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogic.Service.Event
{
    public class EventStatusExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventStatusExpirationService> _logger;

        public EventStatusExpirationService(IServiceScopeFactory serviceScopeFactory, ILogger<EventStatusExpirationService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessExpiredEventsAsync(stoppingToken);

                try
                {
                    // Check every 5 minutes
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public async Task<int> ProcessExpiredEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var now = DataAccess.Helper.DateTimeHelper.GetVietnamTime();

                var expiredEvents = await uow.Events.GetAllAsync(e =>
                    e.Status == EventStatusEnum.Pending && e.StartTime < now && e.DeletedAt == null);

                if (expiredEvents.Any())
                {
                    foreach (var ev in expiredEvents)
                    {
                        ev.Status = EventStatusEnum.Expired;
                        ev.UpdatedAt = now;
                        await uow.Events.UpdateAsync(ev);
                    }

                    await uow.SaveChangesAsync();
                    _logger.LogInformation("Automatically marked {ExpiredCount} pending events as Expired.", expiredEvents.Count());
                }

                return expiredEvents.Count();
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to process expired events.");
                return 0;
            }
        }
    }
}
