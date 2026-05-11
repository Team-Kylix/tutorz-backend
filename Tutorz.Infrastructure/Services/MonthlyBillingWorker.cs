using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Services
{
    /// <summary>
    /// Background service that triggers monthly bill generation on the 1st of each month.
    /// Runs at 02:00 AM Sri Lanka Time to ensure all usage from the previous month is finalised.
    /// </summary>
    public class MonthlyBillingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyBillingWorker> _logger;
        private static readonly TimeZoneInfo SriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");

        public MonthlyBillingWorker(IServiceProvider serviceProvider, ILogger<MonthlyBillingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monthly Billing Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowLkt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SriLankaTimeZone);

                    // Trigger on the 1st day of the month between 02:00 and 03:00 AM
                    if (nowLkt.Day == 1 && nowLkt.Hour == 2)
                    {
                        // Calculate previous month and year
                        var targetMonth = nowLkt.Month == 1 ? 12 : nowLkt.Month - 1;
                        var targetYear = nowLkt.Month == 1 ? nowLkt.Year - 1 : nowLkt.Year;

                        _logger.LogInformation("Triggering monthly bill generation for {Month}/{Year}...", targetMonth, targetYear);

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                            await billService.RolloverOverdueBillsAsync(targetMonth, targetYear);
                        }

                        _logger.LogInformation("Monthly bill generation completed successfully.");

                        // Wait for an hour to avoid multiple triggers in the same hour window
                        await Task.Delay(TimeSpan.FromHours(1.1), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Monthly Billing Worker.");
                }

                // Check every 30 minutes
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }

            _logger.LogInformation("Monthly Billing Worker stopped.");
        }
    }
}
