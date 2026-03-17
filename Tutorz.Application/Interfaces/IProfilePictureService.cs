using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IProfilePictureService
    {
        Task<(string smallUrl, string largeUrl)> UploadProfilePictureAsync(Guid entityId, string registrationNumber, string role, IFormFile file);
    }
}
