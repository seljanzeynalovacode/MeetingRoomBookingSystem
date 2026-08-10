namespace MRBSystem.Contracts.DTOs.FloorDtos;

public record ResponseFloorDto : BaseDto
{
    public string Name { get; set; } = null!;
    public int LevelNumber { get; set; }
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
}
