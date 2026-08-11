namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; }
}
