using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = null!;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = null!;
}
