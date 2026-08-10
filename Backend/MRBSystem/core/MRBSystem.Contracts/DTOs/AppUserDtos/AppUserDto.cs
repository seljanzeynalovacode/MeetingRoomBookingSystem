namespace MRBSystem.Contracts.DTOs.AppUserDtos;

public record AppUserDto : BaseDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public ICollection<string> Roles { get; set; } = new List<string>();
}
