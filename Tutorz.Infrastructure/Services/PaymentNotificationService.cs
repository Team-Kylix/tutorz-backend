using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Tutorz.Infrastructure.Services
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PaymentNotificationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task SendPaymentSuccessNotificationAsync(Guid classPaymentId)
        {
            // We use a new scope because this may be running in a background task
            // where the original HTTP request scope is already disposed.
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var payment = await context.ClassPayments
                .Include(p => p.Student)
                    .ThenInclude(s => s.User)
                .Include(p => p.Class)
                .FirstOrDefaultAsync(p => p.PaymentId == classPaymentId);

            if (payment == null || payment.Student?.User == null || payment.Class == null)
                return;

            var user = payment.Student.User;
            var monthName = new DateTime(payment.Year, payment.Month, 1).ToString("MMMM");
            var className = payment.Class.ClassName ?? payment.Class.Subject;
            var amount = payment.AmountPaid.ToString("N2");
            var date = payment.PaidAt.ToString("yyyy-MM-dd HH:mm");

            // 1. In-App Notification
            string title = "Payment Successful";
            string inAppMsg = $"Your payment of LKR {amount} for {className} was successful.";
            
            try 
            {
                await notificationService.CreateAndPushAsync(user.UserId, title, inAppMsg, "Payment", payment.PaymentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PaymentNotificationService] In-App Notification failed: {ex.Message}");
            }

            // 2. SMS Notification
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                string smsMessage = $"Hi {payment.Student.FirstName}, your payment of LKR {amount} for {className} ({monthName} {payment.Year}) has been received successfully. Thank you!";
                try
                {
                    await smsService.SendSmsAsync(user.PhoneNumber, smsMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PaymentNotificationService] SMS failed: {ex.Message}");
                }
            }

            // 3. Email Notification
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                string subject = "Class Fee Payment Successful";
                string emailHtml = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                  <h2 style='color: #4CAF50;'>Payment Received Successfully!</h2>
                  <p>Dear <strong>{payment.Student.FirstName} {payment.Student.LastName}</strong>,</p>
                  <p>We have successfully received your class fee payment. Here are your payment details:</p>
                  <table style='width: 100%; max-width: 400px; border-collapse: collapse; margin-top: 10px;'>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Class:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{className}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Month:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{monthName} {payment.Year}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Amount Paid:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>LKR {amount}</td></tr>
                    <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><strong>Date:</strong></td><td style='padding: 8px; border-bottom: 1px solid #ddd;'>{date}</td></tr>
                  </table>
                  <p style='margin-top: 20px;'>Thank you for your prompt payment.</p>
                  <p>Best regards,<br/>The Tutorz Team</p>
                </div>";

                try
                {
                    emailService.SendEmail(user.Email, subject, emailHtml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PaymentNotificationService] Email failed: {ex.Message}");
                }
            }
        }
    }
}
