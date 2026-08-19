using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record RefreshTokenRequestDto
{
    [Required, StringLength(500)]
    public string RefreshToken { get; set; } = null!;
}