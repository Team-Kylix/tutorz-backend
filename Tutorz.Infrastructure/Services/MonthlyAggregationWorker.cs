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

namespace Tutorz.Infrastructure.Services
{
    public class MonthlyAggregationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyAggregationWorker> _logger;

        public MonthlyAggregationWorker(IServiceProvider serviceProvider, ILogger<MonthlyAggregationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MonthlyAggregationWorker starting... Waiting 3 minutes for Azure app to warm up.");

            // Prevent thread pool starvation on startup by delaying the first run
            // Staggered 1 minute after the Daily worker to prevent DB query competition
            try { await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAggregationAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during monthly aggregation.");
                }

                // Check again in 1 hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessAggregationAsync()
        {
            var today = DateTime.UtcNow;

            // We aggregate the previous month into the summary
            var previousMonthDate = today.AddMonths(-1);
            var yearMonthStr = previousMonthDate.ToString("yyyy-MM");

            // Define borders of the previous month
            var startDate = new DateTime(previousMonthDate.Year, previousMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();

            // Idempotency: Check if this specific month has already been aggregated
            var alreadyAggregated = await dbContext.APIMonthlyUsageSummaries
                .AnyAsync(s => s.MonthYear == yearMonthStr);

            if (alreadyAggregated)
            {
                return; // Nothing to do, already aggregated
            }

            // Also check if we should actually aggregate. We should only aggregate if we're actually past that month.
            // (today > endDate) is true because previousMonthDate is calculated precisely to ensure we shifted to a new month.
            
            _logger.LogInformation($"Aggregating monthly API usage for {yearMonthStr} (From {startDate} to {endDate}) using daily summaries...");

            var aggregatedData = await dbContext.ApiDailyUsageSummaries
                .Where(log => log.Date >= startDate && log.Date <= endDate)
                .GroupBy(log => log.UserId)
                .Select(g => new APIMonthlyUsageSummary
                {
                    UserId = g.Key,
                    MonthYear = yearMonthStr,
                    TotalCalls = g.Sum(x => x.TotalCalls),
                    StartDate = startDate,
                    EndDate = endDate
                })
                .ToListAsync();

            if (aggregatedData.Any())
            {
                await dbContext.APIMonthlyUsageSummaries.AddRangeAsync(aggregatedData);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Added {aggregatedData.Count} monthly usage summaries for {yearMonthStr}.");
            }
        }
    }
}
