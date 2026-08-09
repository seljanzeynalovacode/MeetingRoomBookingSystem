using MRBSystem.Domain.Enums;

namespace MRBSystem.Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid? RezervationId { get; set; }
    public Reservation? Rezervation { get; set; }
    public DateTime ScheduledFor { get;set; }
    public DateTime? SentAt { get; set; }
    public NotificationType Type { get; set; }
}
