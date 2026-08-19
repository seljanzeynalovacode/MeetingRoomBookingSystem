using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.ParticipantDtos;

public record CreateParticipantDto
{
    public Guid ReservationId { get; set; }
    public Guid? UserId { get; set; }

    [EmailAddress, StringLength(256)]
    public string? GuestEmail { get; set; }

    [StringLength(200, MinimumLength = 2)]
    public string? GuestName { get; set; }
}