using System;
using System.Drawing; 
using System.IO;
using System.Threading.Tasks;
using QRCoder;
using Tutorz.Application.Interfaces;
using Microsoft.AspNetCore.Hosting; 

namespace Tutorz.Application.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly IWebHostEnvironment _environment;

        public QrCodeService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> GenerateUserQrCodeAsync(string registrationId, string name, string mobile, string role)
        {
            // Define the Data Payload
            // You can format this as a JSON string, a vCard, or just plain text.
            // Requirement: ID, Name, Mobile, Login URL
            string loginUrl = "https://tutorz.app/login"; // Replace with your actual frontend URL
            string payload = $"ID: {registrationId}\nName: {name}\nMobile: {mobile}\nRole: {role}\nLogin: {loginUrl}";

            // Generate QR Code Object
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    // Get image bytes
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);

                    // Define File Path
                    // We save inside "wwwroot/qrcodes"
                    string folderPath = Path.Combine(_environment.WebRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", "qrcodes");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = $"{registrationId}_{Guid.NewGuid().ToString().Substring(0, 8)}.png";
                    string fullPath = Path.Combine(folderPath, fileName);

                    // Save to Disk
                    await File.WriteAllBytesAsync(fullPath, qrCodeBytes);

                    // Return the Relative URL (to be saved in DB)
                    // This allows frontend to access it like: https://api.tutorz.com/qrcodes/INS26100001_....png
                    return $"/qrcodes/{fileName}";
                }
            }
        }
    }
}