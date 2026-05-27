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
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.Now;

            var flightsToComplete = await dbContext.Flights
                .Where(f => f.Status != FlightStatus.Completed && f.Status != FlightStatus.Cancelled && f.ArrivalDt <= now)
                .ToListAsync(cancellationToken);

            if (!flightsToComplete.Any())
            {
                _logger.LogDebug("Нет рейсов для обновления статуса на Completed.");
                return;
            }

            foreach (var flight in flightsToComplete)
            {
                flight.Status = FlightStatus.Completed;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Обновлено {Count} рейсов до статуса Completed.", flightsToComplete.Count);
        }
    }
}
