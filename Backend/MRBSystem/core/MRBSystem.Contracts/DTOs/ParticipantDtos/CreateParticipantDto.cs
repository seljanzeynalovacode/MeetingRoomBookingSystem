namespace MRBSystem.Contracts.DTOs.ParticipantDtos;

public record CreateParticipantDto
{
    public Guid ReservationId { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
}
