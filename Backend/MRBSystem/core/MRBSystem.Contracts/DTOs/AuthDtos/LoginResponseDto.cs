namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record LoginResponseDto
{
    public bool Requires2FA { get; init; }
    public TokenResponseDto? Tokens { get; init; } 
    public string? Message { get; init; }
}