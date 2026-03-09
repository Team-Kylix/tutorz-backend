using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;
using Tutorz.Infrastructure.Data;
using System.Linq;

namespace Tutorz.Infrastructure.Services
{
    public class ApiUsageBatchWorker : BackgroundService
    {
        private readonly ApiUsageTracker _tracker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiUsageBatchWorker> _logger;
        private readonly TimeSpan _flushInterval = TimeSpan.FromMinutes(5);

        public ApiUsageBatchWorker(ApiUsageTracker tracker, IServiceProvider serviceProvider, ILogger<ApiUsageBatchWorker> logger)
        {
            _tracker = tracker;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ApiUsageBatchWorker starting...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Delay until the flush interval, honoring the stopping token so it wakes up when shutdown occurs
                    await Task.Delay(_flushInterval, stoppingToken);

                    // Reached flush interval, flush to database
                    await FlushAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when Task.Delay is cancelled via stoppingToken during shutdown
            }
            catch (OperationCanceledException)
            {
                // Expected during graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ApiUsageBatchWorker while running.");
            }
            finally
            {
                // When stoppingToken is triggered, ensure we flush the remaining logs before shutdown finishes
                _logger.LogInformation("ApiUsageBatchWorker gracefully stopping. Flushing remaining logs to Db...");
                await FlushAsync();
            }
        }

        private async Task FlushAsync()
        {
            var logs = _tracker.DequeueAll();

            if (!logs.Any()) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();

                await dbContext.ApiUsageLogs.AddRangeAsync(logs);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"Flushed {logs.Count} API usage logs to the database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush API usage logs.");
            }
        }
    }
}
