using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IQrCodeService
    {
        Task<string> GenerateUserQrCodeAsync(string customId, string name, string mobile, string role);
    }
}
