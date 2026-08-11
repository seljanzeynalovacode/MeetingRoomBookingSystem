using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.BuildingDtos;

public record ResponseBuildingDto : BaseDto
{
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
}
