using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.Services;
using System.Net.Http.Json;

namespace MRBSystem.Infrasturucture.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    // Emails are sent via Brevo's HTTPS transactional API (not SMTP), because many
    // hosts (e.g. Render free tier) block outbound SMTP ports.
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var apiKey = _configuration["Brevo:ApiKey"]!;
            var fromEmail = _configuration["Smtp:FromEmail"]!;
            var fromName = _configuration["Smtp:FromName"] ?? "MRBSystem";

            var payload = new
            {
                sender = new { name = fromName, email = fromEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                textContent = body
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Brevo email failed to {ToEmail}. Status: {Status}. Body: {Body}",
                    toEmail, response.StatusCode, errorBody);
                throw new InvalidOperationException(
                    $"Email send failed with status {response.StatusCode}.");
            }

            _logger.LogInformation("Email sent to {ToEmail} via Brevo.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            throw;
        }
    }
}
