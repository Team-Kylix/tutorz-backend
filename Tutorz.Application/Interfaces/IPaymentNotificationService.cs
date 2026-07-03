using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IPaymentNotificationService
    {
        Task SendPaymentSuccessNotificationAsync(Guid classPaymentId);
    }
}
