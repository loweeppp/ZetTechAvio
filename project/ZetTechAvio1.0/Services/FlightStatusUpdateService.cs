using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZetTechAvio1._0.Data;
using ZetTechAvio1._0.Models;

namespace ZetTechAvio1._0.Services
{
    public class FlightStatusUpdateService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FlightStatusUpdateService> _logger;

        public FlightStatusUpdateService(IServiceScopeFactory scopeFactory, ILogger<FlightStatusUpdateService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateCompletedFlightsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении статусов завершённых рейсов.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task UpdateCompletedFlightsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var flightsService = scope.ServiceProvider.GetRequiredService<IFlightsService>();
            var now = DateTime.UtcNow;

            var updatedCount = await flightsService.MarkPastFlightsCompletedAsync();

            if (updatedCount == 0)
            {
                _logger.LogDebug("[{NowUtc}] Нет рейсов для обновления статуса на Completed.", now);
                return;
            }

            _logger.LogInformation("[{NowUtc}] Обновлено {Count} рейсов до статуса Completed.", now, updatedCount);
        }
    }
}
