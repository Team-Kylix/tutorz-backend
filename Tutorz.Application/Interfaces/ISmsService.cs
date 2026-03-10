using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string to, string message, Guid? senderUserId = null);
    }
}
