using MRBSystem.Contracts.DTOs.NotificationDtos;

namespace MRBSystem.Contracts.Services;

public interface INotificationService
{
    Task<ICollection<ResponseNotificationDto>> GetMyNotificationsAsync();
    Task MarkAsReadAsync(Guid id);
}
