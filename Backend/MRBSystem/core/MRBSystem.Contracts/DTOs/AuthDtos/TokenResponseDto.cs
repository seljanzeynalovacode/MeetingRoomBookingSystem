namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record TokenResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

}
