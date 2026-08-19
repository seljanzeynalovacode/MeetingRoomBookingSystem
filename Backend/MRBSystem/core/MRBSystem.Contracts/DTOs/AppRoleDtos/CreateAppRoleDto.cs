using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.AppRoleDtos;

public record CreateAppRoleDto
{
    [Required, StringLength(256, MinimumLength = 2)]
    public string Name { get; set; } = null!;
}