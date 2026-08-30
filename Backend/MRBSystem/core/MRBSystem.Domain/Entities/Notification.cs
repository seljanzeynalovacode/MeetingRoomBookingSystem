using MRBSystem.Domain.Enums;

namespace MRBSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }
    public string? GuestEmail { get; set; }
    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public NotificationType Type { get; set; }
}
