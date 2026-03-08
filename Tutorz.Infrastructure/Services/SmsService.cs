using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SmsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> SendSmsAsync(string to, string message)
        {
            try
            {
                var apiUrl = _configuration["SmsSettings:ApiUrl"] ?? "https://app.text.lk/api/v3/sms/send";
                var apiToken = _configuration["SmsSettings:ApiToken"]; // Retrieve token from appsettings
                var senderId = _configuration["SmsSettings:SenderId"] ?? "TextLKDemo";

                if (string.IsNullOrEmpty(apiToken))
                {
                    Console.WriteLine("SMS sending skipped: API Token is not configured.");
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
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send SMS to {to}. Status: {response.StatusCode}, Error: {errorBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when sending SMS to {to}: {ex.Message}");
                return false;
            }
        }
    }
}
