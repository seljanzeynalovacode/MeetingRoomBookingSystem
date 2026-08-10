namespace MRBSystem.Contracts.DTOs.CheckinDtos;

public record CreateCheckinDto
{
    public string QrCodeToken { get; set; } = null!;
}
