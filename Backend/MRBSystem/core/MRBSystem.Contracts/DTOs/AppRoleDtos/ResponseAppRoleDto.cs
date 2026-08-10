namespace MRBSystem.Contracts.DTOs.AppRoleDtos;

public record ResponseAppRoleDto : BaseDto
{
    public string Name { get; set; } = null!;
}
