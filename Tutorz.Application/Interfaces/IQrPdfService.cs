using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IQrPdfService
    {
        Task<byte[]> GenerateClassQrCodesPdfAsync(Guid classId);
        Task<byte[]> GenerateUserQrPdfAsync(Guid userId);
    }
}
