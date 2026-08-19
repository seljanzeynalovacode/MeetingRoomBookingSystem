using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public class ResetPasswordDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    public string Token { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = null!;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = null!;
}
