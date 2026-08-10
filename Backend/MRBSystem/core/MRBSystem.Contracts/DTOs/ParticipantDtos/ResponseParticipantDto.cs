namespace MRBSystem.Contracts.DTOs.ParticipantDtos;

public record ResponseParticipantDto : BaseDto
{
    public Guid ReservationId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
    public string ResponseStatus { get; set; } = null!;
}
