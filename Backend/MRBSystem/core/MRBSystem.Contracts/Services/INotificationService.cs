using MRBSystem.Contracts.DTOs.NotificationDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;

namespace MRBSystem.Contracts.Services;

public interface INotificationService
{
    Task<ICollection<ResponseNotificationDto>> GetMyNotificationsAsync();
    Task MarkAsReadAsync(Guid id);
    Task CreateAsync(Reservation reservation, NotificationType type);
}
