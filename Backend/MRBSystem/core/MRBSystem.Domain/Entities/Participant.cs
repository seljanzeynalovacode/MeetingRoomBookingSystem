using MRBSystem.Domain.Enums;

namespace MRBSystem.Domain.Entities;

public class Participant : BaseEntity
{
    public Guid RezervationId { get; set; }
    public Reservation Rezervation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; } = null!;
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
    public ResponseStatus ResponseStatus { get; set; } = ResponseStatus.Invited;

}
