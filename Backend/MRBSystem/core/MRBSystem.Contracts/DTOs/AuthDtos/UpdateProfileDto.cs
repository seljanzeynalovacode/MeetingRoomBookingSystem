using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AuthDtos;

public class UpdateProfileDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = null!;

    [Required, StringLength(256, MinimumLength = 3)]
    public string UserName { get; set; } = null!;
}
