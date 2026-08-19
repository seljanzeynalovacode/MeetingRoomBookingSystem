using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = null!;

    [Required, StringLength(256, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = null!;
}