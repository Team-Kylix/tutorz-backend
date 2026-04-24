using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.IO;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Infrastructure.Services
{
    public class ProfilePictureService : IProfilePictureService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IInstituteRepository _instituteRepository;

        // Target dimensions
        private const int SmallWidth = 150;
        private const int SmallHeight = 150;
        private const int LargeWidth = 600;
        private const int LargeHeight = 600;

        public ProfilePictureService(
            IWebHostEnvironment env, 
            IConfiguration configuration,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IInstituteRepository instituteRepository)
        {
            _env = env;
            _configuration = configuration;
            _studentRepository = studentRepository;
            _tutorRepository = tutorRepository;
            _instituteRepository = instituteRepository;
        }

        public async Task<(string smallUrl, string largeUrl)> UploadProfilePictureAsync(Guid entityId, string registrationNumber, string role, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            string smallFileName = $"{registrationNumber}_small.jpg";
            string largeFileName = $"{registrationNumber}_large.jpg";

            string smallUrl;
            string largeUrl;

            // Generate resized image streams
            using var smallImageStream = await ResizeImageAsync(memoryStream.ToArray(), SmallWidth, SmallHeight);
            using var largeImageStream = await ResizeImageAsync(memoryStream.ToArray(), LargeWidth, LargeHeight);

            if (_env.IsDevelopment())
            {
                // Local Dev Save
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "profile-pictures");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string smallFilePath = Path.Combine(uploadsFolder, smallFileName);
                string largeFilePath = Path.Combine(uploadsFolder, largeFileName);

                using (var fileStream = new FileStream(smallFilePath, FileMode.Create))
                {
                    await smallImageStream.CopyToAsync(fileStream);
                }

                using (var fileStream = new FileStream(largeFilePath, FileMode.Create))
                {
                    largeImageStream.Position = 0;
                    await largeImageStream.CopyToAsync(fileStream);
                }

                // Assuming host is localhost, we construct relative paths
                smallUrl = $"/profile-pictures/{smallFileName}";
                largeUrl = $"/profile-pictures/{largeFileName}";
            }
            else
            {
                // Production Azure Blob Save
                string connectionString = _configuration.GetConnectionString("AzureBlobStorage");
                if (string.IsNullOrEmpty(connectionString))
                    throw new Exception("AzureBlobStorage connection string is not configured.");

                string containerName = "profile-pictures";
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure container exists and is public
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Upload Small
                var smallBlobClient = containerClient.GetBlobClient(smallFileName);
                smallImageStream.Position = 0;
                await smallBlobClient.UploadAsync(smallImageStream, new BlobHttpHeaders { ContentType = "image/jpeg" });
                smallUrl = smallBlobClient.Uri.ToString();

                // Upload Large
                var largeBlobClient = containerClient.GetBlobClient(largeFileName);
                largeImageStream.Position = 0;
                await largeBlobClient.UploadAsync(largeImageStream, new BlobHttpHeaders { ContentType = "image/jpeg" });
                largeUrl = largeBlobClient.Uri.ToString();
            }

            // Update Database Entity
            await UpdateEntityProfileUrlsAsync(entityId, role, smallUrl, largeUrl);

            return (smallUrl, largeUrl);
        }

        private async Task<MemoryStream> ResizeImageAsync(byte[] imageBytes, int width, int height)
        {
            var outputStream = new MemoryStream();
            using (var image = Image.Load(imageBytes))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Crop
                }));

                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 });
            }
            outputStream.Position = 0;
            return outputStream;
        }

        private async Task UpdateEntityProfileUrlsAsync(Guid entityId, string role, string smallUrl, string largeUrl)
        {
            switch (role.ToLower())
            {
                case "student":
                    var student = await _studentRepository.GetAsync(x => x.StudentId == entityId || x.UserId == entityId);
                    if (student != null)
                    {
                        student.ProfileImageUrlSmall = smallUrl;
                        student.ProfileImageUrlLarge = largeUrl;
                        await _studentRepository.SaveChangesAsync();
                    }
                    break;
                case "tutor":
                    var tutor = await _tutorRepository.GetAsync(x => x.TutorId == entityId || x.UserId == entityId);
                    if (tutor != null)
                    {
                        tutor.ProfileImageUrlSmall = smallUrl;
                        tutor.ProfileImageUrlLarge = largeUrl;
                        await _tutorRepository.SaveChangesAsync();
                    }
                    break;
                case "institute":
                    var institute = await _instituteRepository.GetAsync(x => x.InstituteId == entityId || x.UserId == entityId);
                    if (institute != null)
                    {
                        institute.ProfileImageUrlSmall = smallUrl;
                        institute.ProfileImageUrlLarge = largeUrl;
                        await _instituteRepository.SaveChangesAsync();
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid role provided for profile picture update.");
            }
        }
    }
}
