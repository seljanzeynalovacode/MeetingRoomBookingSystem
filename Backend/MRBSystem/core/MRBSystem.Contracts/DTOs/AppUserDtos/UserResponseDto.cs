using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.AppUserDtos;

public record UserResponseDto 
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public ICollection<string> Roles { get; set; } = new List<string>();
}
