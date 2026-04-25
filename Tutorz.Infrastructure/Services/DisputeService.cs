using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Disputes;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Tutorz.Infrastructure.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly TutorzDbContext _dbContext;

        public DisputeService(
            IDisputeRepository disputeRepository,
            IWebHostEnvironment env,
            IConfiguration configuration,
            INotificationService notificationService,
            TutorzDbContext dbContext)
        {
            _disputeRepository   = disputeRepository;
            _env                 = env;
            _configuration       = configuration;
            _notificationService = notificationService;
            _dbContext           = dbContext;
        }

        public async Task<ServiceResponse<DisputeResponseDto>> CreateDisputeAsync(Guid userId, CreateDisputeDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return Fail<DisputeResponseDto>("Title is required.");

                if (string.IsNullOrWhiteSpace(dto.Description))
                    return Fail<DisputeResponseDto>("Description is required.");

                // Generate sequential dispute number BEFORE saving so the blob name is consistent
                var disputeNumber = await _disputeRepository.GenerateDisputeNumberAsync();

                string? screenshotUrl = null;
                if (dto.Screenshot != null && dto.Screenshot.Length > 0)
                {
                    screenshotUrl = await UploadScreenshotAsync(dto.Screenshot, disputeNumber);
                }

                var dispute = new Dispute
                {
                    DisputeNumber  = disputeNumber,
                    Title          = dto.Title.Trim(),
                    Description    = dto.Description.Trim(),
                    Category       = dto.Category,
                    ScreenshotUrl  = screenshotUrl,
                    RaisedByUserId = userId,
                    CreatedAt      = DateTime.UtcNow,
                    UpdatedAt      = DateTime.UtcNow
                };

                await _disputeRepository.AddAsync(dispute);
                await _disputeRepository.SaveChangesAsync();

                var result = await _disputeRepository.GetDisputeByIdAsync(dispute.Id);

                // --- Trigger Notifications ---
                try
                {
                    // 1. Notify the Submitter
                    await _notificationService.CreateAndPushAsync(
                        userId, 
                        $"Complaint Submitted ({disputeNumber})", 
                        "Your complaint has been successfully submitted and is pending review.", 
                        "Dispute");

                    // 2. Notify all Admins
                    var adminUserIds = await _dbContext.Users
                        .Where(u => _dbContext.Roles.Any(r => r.RoleId == u.RoleId && r.Name == "Admin"))
                        .Select(u => u.UserId)
                        .ToListAsync();

                    foreach(var adminId in adminUserIds)
                    {
                        await _notificationService.CreateAndPushAsync(
                            adminId,
                            "New Complaint Logged",
                            $"A new complaint ({disputeNumber}) has been logged and requires review.",
                            "Dispute");
                    }
                }
                catch (Exception hookEx)
                {
                    // Non-fatal, just log in real system
                }

                return new ServiceResponse<DisputeResponseDto>
                {
                    Success = true,
                    Data    = result!,
                    Message = "Complaint submitted successfully."
                };
            }
            catch (Exception ex)
            {
                return Fail<DisputeResponseDto>($"Failed to submit complaint: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<DisputeResponseDto>> GetDisputeByIdAsync(int disputeId, Guid callerUserId, bool isAdmin)
        {
            var dispute = await _disputeRepository.GetDisputeByIdAsync(disputeId);
            if (dispute == null)
                return Fail<DisputeResponseDto>("Dispute not found.");

            if (!isAdmin && dispute.RaisedByUserId != callerUserId)
                return Fail<DisputeResponseDto>("You do not have permission to view this dispute.");

            return new ServiceResponse<DisputeResponseDto> { Success = true, Data = dispute };
        }

        public async Task<ServiceResponse<PaginatedResultDto<DisputeResponseDto>>> GetMyDisputesAsync(Guid userId, int page, int pageSize)
        {
            var result = await _disputeRepository.GetDisputesByUserIdAsync(userId, page, pageSize);
            return new ServiceResponse<PaginatedResultDto<DisputeResponseDto>> { Success = true, Data = result };
        }

        public async Task<ServiceResponse<PaginatedResultDto<DisputeResponseDto>>> GetAllDisputesAsync(string? searchQuery, int page, int pageSize)
        {
            var result = await _disputeRepository.GetAllDisputesAsync(searchQuery, page, pageSize);
            return new ServiceResponse<PaginatedResultDto<DisputeResponseDto>> { Success = true, Data = result };
        }

        public async Task<ServiceResponse<bool>> UpdateDisputeStatusAsync(int disputeId, UpdateDisputeStatusDto dto)
        {
            var updated = await _disputeRepository.UpdateStatusAsync(disputeId, dto);
            if (!updated)
                return Fail<bool>("Dispute not found.");

            try
            {
                var disputeDetails = await _disputeRepository.GetDisputeByIdAsync(disputeId);
                if (disputeDetails != null)
                {
                    var title = $"Complaint Update ({disputeDetails.DisputeNumber})";
                    var msg = $"Your complaint status has been updated to {disputeDetails.StatusLabel}.";
                    if (!string.IsNullOrWhiteSpace(disputeDetails.AdminNote)) {
                        msg += " An admin has left a note regarding your complaint.";
                    }

                    await _notificationService.CreateAndPushAsync(
                        disputeDetails.RaisedByUserId,
                        title,
                        msg,
                        "Dispute");
                }
            }
            catch { /* Ignore notification failures */ }

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Status updated successfully." };
        }

        // ─── Screenshot Upload ─────────────────────────────────────────────────
        private async Task<string> UploadScreenshotAsync(Microsoft.AspNetCore.Http.IFormFile file, string disputeNumber)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!Array.Exists(allowed, e => e == extension))
                throw new ArgumentException("Only image files (JPG, PNG, GIF, WEBP) are allowed.");

            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("Screenshot must be smaller than 10 MB.");

            var fileName = $"dispute-{disputeNumber}-{Guid.NewGuid():N}{extension}";

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (_env.IsDevelopment())
            {
                var folder = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    "dispute-screenshots");
                Directory.CreateDirectory(folder);

                using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                await stream.CopyToAsync(fs);

                return $"/dispute-screenshots/{fileName}";
            }
            else
            {
                var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception("AzureBlobStorage connection string is not configured.");

                var containerName   = "dispute-screenshots";
                var blobService     = new BlobServiceClient(connectionString);
                var containerClient = blobService.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(fileName);
                stream.Position = 0;
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
                return blobClient.Uri.ToString();
            }
        }

        // ─── Helper ─────────────────────────────────────────────────────────────
        private static ServiceResponse<T> Fail<T>(string message) =>
            new ServiceResponse<T> { Success = false, Message = message };
    }
}
