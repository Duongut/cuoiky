using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class MaintenanceService : BackgroundService
    {
        private readonly ILogger<MaintenanceService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public MaintenanceService(
            ILogger<MaintenanceService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Maintenance Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Maintenance Service is running maintenance tasks.");

                try
                {
                    // Update expired monthly vehicles
                    await UpdateExpiredMonthlyVehicles();

                    // Add other maintenance tasks here as needed
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running maintenance tasks.");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Maintenance Service is stopping.");
        }

        private async Task UpdateExpiredMonthlyVehicles()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var monthlyVehicleService = scope.ServiceProvider.GetRequiredService<MonthlyVehicleService>();
                    await monthlyVehicleService.UpdateExpiredVehiclesAsync();
                    _logger.LogInformation("Successfully updated expired monthly vehicles.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expired monthly vehicles.");
            }
        }
    }
}
