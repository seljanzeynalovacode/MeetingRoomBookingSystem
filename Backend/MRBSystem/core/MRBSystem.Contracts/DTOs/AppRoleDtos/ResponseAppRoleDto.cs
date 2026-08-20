namespace MRBSystem.Contracts.DTOs.AppRoleDtos;

public record ResponseAppRoleDto 
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
