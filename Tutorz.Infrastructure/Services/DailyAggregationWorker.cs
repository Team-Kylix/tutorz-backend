using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using System;
using Tutorz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tutorz.Domain.Entities;
using System.Linq;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Services
{
    public class DailyAggregationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyAggregationWorker> _logger;

        public DailyAggregationWorker(IServiceProvider serviceProvider, ILogger<DailyAggregationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyAggregationWorker starting... Waiting 2 minutes for Azure app to warm up.");
            
            // Prevent thread pool starvation on startup by delaying the first run
            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDailyAggregationAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during daily aggregation.");
                }

                // Check again in 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessDailyAggregationAsync()
        {
            var now = DateTime.UtcNow;

            // Wait until 2:00 AM (or shortly after)
            if (now.Hour < 2)
            {
                return;
            }

            // We want to aggregate the data for *yesterday*
            var yesterday = now.Date.AddDays(-1);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();

            // Idempotency: Check if yesterday has already been aggregated
            var alreadyAggregated = await dbContext.ApiDailyUsageSummaries
                .AnyAsync(s => s.Date.Date == yesterday.Date);

            if (alreadyAggregated)
            {
                return; // Already done for yesterday
            }

            _logger.LogInformation($"Aggregating daily API usage for {yesterday.Date:yyyy-MM-dd}...");

            // Border times for yesterday
            var startDate = yesterday.Date;
            var endDate = startDate.AddDays(1).AddTicks(-1);

            var aggregatedData = await dbContext.ApiUsageLogs
                .Where(log => log.Timestamp >= startDate && log.Timestamp <= endDate && log.UserId != null)
                .GroupBy(log => log.UserId)
                .Select(g => new ApiDailyUsageSummary
                {
                    UserId = g.Key.Value,
                    Date = startDate,
                    TotalCalls = g.Count()
                })
                .ToListAsync();

            if (aggregatedData.Any())
            {
                await dbContext.ApiDailyUsageSummaries.AddRangeAsync(aggregatedData);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Added {aggregatedData.Count} daily usage summaries for {yesterday.Date:yyyy-MM-dd}.");

                // Increment real-time bill for all users involved
                var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                foreach (var data in aggregatedData)
                {
                    await billService.IncrementApiUsageAsync(data.UserId, data.TotalCalls, data.Date);
                }
            }

            // Clean up: delete raw logs from yesterday since they are now aggregated
            // To be safe, maybe we should keep a short buffer (like 7 days of raw logs for debugging), 
            // but if we want to save space immediately, we delete them here. Let's keep 7 days of raw logs just in case.
            var deleteThreshold = now.Date.AddDays(-7);
            var oldLogs = dbContext.ApiUsageLogs.Where(log => log.Timestamp < deleteThreshold);
            if (await oldLogs.AnyAsync())
            {
                dbContext.ApiUsageLogs.RemoveRange(oldLogs);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted raw API usage logs older than 7 days.");
            }
        }
    }
}
