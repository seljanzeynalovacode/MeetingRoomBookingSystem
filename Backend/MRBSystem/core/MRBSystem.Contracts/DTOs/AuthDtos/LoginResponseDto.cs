namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record LoginResponseDto
{
    public bool Requires2FA { get; init; }
    public TokenResponseDto? Tokens { get; init; } // Əgər 2FA tələb olunmazsa (məs: User login-də) birbaşa token qayıdır
    public string? Message { get; init; }
}