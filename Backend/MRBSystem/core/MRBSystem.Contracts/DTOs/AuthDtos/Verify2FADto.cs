namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record Verify2FADto
{
    public string Email { get; init; } = null!;
    public string Code { get; init; } = null!;
    public bool RememberMe { get; init; }
}
