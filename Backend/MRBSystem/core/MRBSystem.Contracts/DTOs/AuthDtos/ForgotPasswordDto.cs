using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record ForgotPasswordDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;
}