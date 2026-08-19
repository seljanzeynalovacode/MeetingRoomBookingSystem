using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record Verify2FADto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = null!;

    [Required, StringLength(6, MinimumLength = 6), RegularExpression(@"^\d{6}$")]
    public string Code { get; init; } = null!;

    public bool RememberMe { get; init; }
}