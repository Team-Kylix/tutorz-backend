using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Tutorz.Application.Services;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using Tutorz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Api.HostedServices
{
    public class ReplenishmentBackgroundService : BackgroundService
    {
        private readonly IReplenishmentQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReplenishmentBackgroundService> _logger;

        public ReplenishmentBackgroundService(
            IReplenishmentQueue queue,
            IServiceProvider serviceProvider,
            ILogger<ReplenishmentBackgroundService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Replenishment Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = await _queue.DequeueAsync(stoppingToken);

                    // We wrap the processing in a try-catch to ensure the loop continues on error
                    try
                    {
                        await ProcessReplenishmentAsync(request, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred processing replenishment for CreatorId: {CreatorId}", request.CreatorId);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Replenishment Background Service loop");
                }
            }

            _logger.LogInformation("Replenishment Background Service is stopping.");
        }

        private async Task ProcessReplenishmentAsync(ReplenishRequest request, CancellationToken stoppingToken)
        {
            // Only Student pre-registrations are supported right now
            var roleToPreAllocate = "Student";

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();
            var idGeneratorService = scope.ServiceProvider.GetRequiredService<IIdGeneratorService>();

            // Double check if there's already an 'Available' one just in case multiple were queued
            var existingAvailable = await dbContext.PreRegistrations
                .AnyAsync(pr => pr.CreatorId == request.CreatorId && pr.Status == 0, stoppingToken);

            if (existingAvailable)
            {
                return; // Already have one ready
            }

            // Generate the new Sequence ID using existing format
            string nextRegNo = await idGeneratorService.GenerateNextIdAsync(roleToPreAllocate, null);

            var preRegistration = new PreRegistration
            {
                Id = Guid.NewGuid(),
                CreatorId = request.CreatorId,
                CreatorRole = request.CreatorRole,
                PreAllocatedRegNo = nextRegNo,
                PreAllocatedUserId = Guid.NewGuid(),
                PreAllocatedStudentId = Guid.NewGuid(),
                Status = 0, // Available
                CreatedAt = DateTime.UtcNow
            };

            dbContext.PreRegistrations.Add(preRegistration);
            await dbContext.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Pre-Allocated Student ID {RegNo} generated for Creator {CreatorId}", nextRegNo, request.CreatorId);
        }
    }
}
