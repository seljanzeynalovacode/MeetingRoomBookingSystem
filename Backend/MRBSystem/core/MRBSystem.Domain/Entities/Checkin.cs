namespace MRBSystem.Domain.Entities;

public class Checkin : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public DateTime CheckinTime { get; set; }
    public DateTime? CheckoutTime { get; set; }
}
