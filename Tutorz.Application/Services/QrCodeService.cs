using System;
using System.Threading.Tasks;
using System.IO;
using QRCoder;
using Tutorz.Application.Interfaces;

namespace Tutorz.Application.Services
{
    public class QrCodeService : IQrCodeService
    {
        public Task<string> GenerateUserQrCodeAsync(string customId, string name, string mobile, string role)
        {
            // Prepare data
            string data = $"ID:{customId}|Name:{name}|Role:{role}";

            // Generate QR Code
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);

                    // Define path
                    // User specified: D:\Tutorz\tutorz-backend\Tutorz.Api\wwwroot\wwwroot\qrcodes
                    // We will attempt to use a relative path logic, but fallback to a predictable structure if needed.
                    // Assuming the API is running with ContentRootPath at D:\Tutorz\tutorz-backend\Tutorz.Api
                    
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "wwwroot", "qrcodes");
                    
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Original filename format example: TUT26201_bfc1e678.png
                    string fileName = $"{customId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.png";
                    string filePath = Path.Combine(folderPath, fileName);

                    File.WriteAllBytes(filePath, qrCodeImage);

                    // Return relative URL
                    // Example: /wwwroot/qrcodes/TUT26201_bfc1e678.png
                    return Task.FromResult($"/wwwroot/qrcodes/{fileName}");
                }
            }
        }
    }
}
