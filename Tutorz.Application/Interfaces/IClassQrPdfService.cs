using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IClassQrPdfService
    {
        Task<byte[]> GenerateClassQrCodesPdfAsync(Guid classId);
    }
}
