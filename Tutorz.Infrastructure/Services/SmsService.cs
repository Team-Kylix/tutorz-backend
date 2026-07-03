using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        public SmsService(HttpClient httpClient, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> SendSmsAsync(string to, string message, Guid? senderUserId = null)
        {
            var smsLog = new SmsLog
            {
                SmsLogId = Guid.NewGuid(),
                SenderUserId = senderUserId,
                ReceiverPhoneNumber = to,
                MessageContent = message,
                SentAt = DateTime.UtcNow,
                Status = "Pending",
                Cost = 2.0m, // Example fixed cost per SMS, you could also pull this from IConfiguration
                ErrorMessage = string.Empty
            };

            bool isSuccess = false;

            try
            {
                var apiUrl = _configuration["SmsSettings:ApiUrl"] ?? "https://app.text.lk/api/v3/sms/send";
                var apiToken = _configuration["SmsSettings:ApiToken"]; // Retrieve token from appsettings
                var senderId = _configuration["SmsSettings:SenderId"] ?? "TextLKDemo";

                if (string.IsNullOrEmpty(apiToken))
                {
                    Console.WriteLine("SMS sending skipped: API Token is not configured.");
                    smsLog.Status = "Failed";
                    smsLog.ErrorMessage = "API Token missing";
                    await SaveLogAsync(smsLog);
                    return false; // Or throw Exception in production
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (to.StartsWith("+"))
                {
                    to = to.Substring(1);
                }

                var payload = new
                {
                    recipient = to,
                    sender_id = senderId,
                    type = "plain",
                    message = message
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"SMS sent successfully to {to}. Response: {responseBody}");
                    smsLog.Status = "Sent";
                    isSuccess = true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send SMS to {to}. Status: {response.StatusCode}, Error: {errorBody}");
                    smsLog.Status = "Failed";
                    smsLog.ErrorMessage = $"Status: {response.StatusCode}, Error: {errorBody}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when sending SMS to {to}: {ex.Message}");
                smsLog.Status = "Failed";
                smsLog.ErrorMessage = ex.Message;
                isSuccess = false;
            }

            await SaveLogAsync(smsLog);

            // Incrementally update bill if sent successfully and sender is known
            if (isSuccess && senderUserId.HasValue)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                    await billService.IncrementSmsUsageAsync(senderUserId.Value, 1, smsLog.Cost, smsLog.SentAt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to update real-time bill for SMS: {ex.Message}");
                }
            }

            return isSuccess;
        }

        private async Task SaveLogAsync(SmsLog log)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TutorzDbContext>();
                
                // Truncate message if too long for db constraints (just a safety precaution)
                if (log.ErrorMessage != null && log.ErrorMessage.Length > 500)
                    log.ErrorMessage = log.ErrorMessage.Substring(0, 497) + "...";

                dbContext.SmsLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save SMS log to database: {ex.Message}");
            }
        }
    }
}
