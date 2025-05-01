using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class TransactionMaintenanceService : BackgroundService
    {
        private readonly ILogger<TransactionMaintenanceService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public TransactionMaintenanceService(
            ILogger<TransactionMaintenanceService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction Maintenance Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Transaction Maintenance Service is checking for timed-out transactions.");

                try
                {
                    // Handle timed-out transactions
                    await HandleTimedOutTransactionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while handling timed-out transactions.");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Transaction Maintenance Service is stopping.");
        }

        private async Task HandleTimedOutTransactionsAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var transactionService = scope.ServiceProvider.GetRequiredService<TransactionService>();
                    await transactionService.HandleTimedOutTransactionsAsync();
                    _logger.LogInformation("Successfully handled timed-out transactions.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling timed-out transactions.");
            }
        }
    }
}
