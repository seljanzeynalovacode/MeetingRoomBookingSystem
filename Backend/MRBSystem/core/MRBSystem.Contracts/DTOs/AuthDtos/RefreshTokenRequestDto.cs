namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = null!;
}
