namespace MRBSystem.Contracts.DTOs.BuildingDtos;

public record UpdateBuildingDto
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
}
