using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            // Add these to your appsettings.json: "EmailSettings": { "From": "...", "SmtpServer": "smtp.gmail.com", "Port": 587, "Username": "...", "Password": "..." }
            var fromEmail = _config["EmailSettings:From"];
            var password = _config["EmailSettings:Password"];

            var client = new SmtpClient(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]))
            {
                Credentials = new NetworkCredential(_config["EmailSettings:Username"], password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            client.Send(mailMessage);
        }
    }
}
