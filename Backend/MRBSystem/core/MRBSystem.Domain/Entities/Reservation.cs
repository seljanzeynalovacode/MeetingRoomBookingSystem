using MRBSystem.Domain.Enums;

namespace MRBSystem.Domain.Entities;

public class Reservation : BaseEntity
{
    public string Title { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public Guid ReservedByUserId { get; set; }
    public AppUser ReservedByUser { get; set; } = null!;
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

}
