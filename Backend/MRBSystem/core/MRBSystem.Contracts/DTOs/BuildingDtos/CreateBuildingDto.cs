namespace MRBSystem.Contracts.DTOs.BuildingDtos;

public record CreateBuildingDto
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
}
