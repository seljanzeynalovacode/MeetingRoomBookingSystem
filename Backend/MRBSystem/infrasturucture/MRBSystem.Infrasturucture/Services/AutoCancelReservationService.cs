using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Infrasturucture.Services;

public class AutoCancelReservationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoCancelReservationService> _logger;

    public AutoCancelReservationService(
        IServiceProvider serviceProvider,
        ILogger<AutoCancelReservationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoCancelReservationService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAutoCancelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing auto-cancel reservations.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessAutoCancelAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var reservationRepository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<Reservation>>();
        var notificationService = scope.ServiceProvider
            .GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        var overdueReservations = await reservationRepository.GetQueryable()
            .Include(r => r.ReservedByUser)
            .Include(r => r.Room)
            .Include(r => r.CheckIn)
            .Where(r => r.Status == ReservationStatus.Confirmed
                && r.CheckIn == null
                && r.CheckinDeadline < now)
            .ToListAsync();

        if (overdueReservations.Count == 0)
            return;

        _logger.LogInformation("Found {Count} overdue reservations to auto-cancel.", overdueReservations.Count);

        foreach (var reservation in overdueReservations)
        {
            reservation.Status = ReservationStatus.AutoCancelled;
            await reservationRepository.UpdateAsync(reservation);

            await notificationService.CreateAndSendAsync(reservation, NotificationType.Cancelled);
        }
    }
 }
