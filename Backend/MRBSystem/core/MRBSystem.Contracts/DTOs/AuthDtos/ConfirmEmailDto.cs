using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public record ConfirmEmailDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    public string ConfirmationCode { get; set; } = null!;
}
