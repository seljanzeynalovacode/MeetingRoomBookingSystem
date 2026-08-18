using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Infrasturucture.Services;

public class NotificationDeliveryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        IServiceProvider serviceProvider,
        ILogger<NotificationDeliveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationDeliveryService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing pending notifications.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessPendingNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var notificationRepository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<Notification>>();
        var emailService = scope.ServiceProvider
            .GetRequiredService<IEmailService>();

        var pendingNotifications = await notificationRepository.GetQueryable()
            .Include(n => n.User)
            .Where(n => n.SentAt == null)
            .OrderBy(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        if (pendingNotifications.Count == 0)
            return;

        _logger.LogInformation("Found {Count} pending notifications to deliver.", pendingNotifications.Count);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                await emailService.SendEmailAsync(
                    notification.User.Email!,
                    notification.Title,
                    notification.Message);

                notification.SentAt = DateTime.UtcNow;
                await notificationRepository.UpdateAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to deliver notification {NotificationId} to {Email}. Will retry on next cycle.",
                    notification.Id, notification.User.Email);
            }
        }
    }
}