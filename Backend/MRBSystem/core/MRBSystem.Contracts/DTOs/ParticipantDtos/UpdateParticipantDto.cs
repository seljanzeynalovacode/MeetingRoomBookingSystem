namespace MRBSystem.Contracts.DTOs.ParticipantDtos;

public record UpdateParticipantDto
{
    public string ResponseStatus { get; set; } = null!;
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
}
