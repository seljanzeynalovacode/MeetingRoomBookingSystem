namespace MRBSystem.Contracts.DTOs.FloorDtos;

public record UpdateFloorDto
{
    public string Name { get; set; } = null!;
    public int LevelNumber { get; set; }
    public Guid BuildingId { get; set; }
}
