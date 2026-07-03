using System.Threading.Tasks;
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
            // We no longer generate or save physical images to disk.
            // The frontend dynamically generates the QR code from the user's RegistrationNumber.
            // Returning the registrationId to use it as the implicit Secret. 
            return await Task.FromResult(registrationId);
        }
    }
}
