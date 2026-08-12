using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.Services;
using System.Net;
using System.Net.Mail;

namespace MRBSystem.Infrasturucture.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var host = _configuration["Smtp:Host"]!;
            var port = int.Parse(_configuration["Smtp:Port"]!);
            var username = _configuration["Smtp:Username"]!;
            var password = _configuration["Smtp:Password"]!;
            var fromEmail = _configuration["Smtp:FromEmail"]!;
            var fromName = _configuration["Smtp:FromName"]!;
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"]!);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
            throw;
        }
    }
}
