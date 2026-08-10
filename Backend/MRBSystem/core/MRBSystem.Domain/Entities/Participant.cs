using MRBSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Domain.Entities;

public class Participant : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; } = null!;
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
    public ResponseStatus ResponseStatus { get; set; } = ResponseStatus.Invited;

}
