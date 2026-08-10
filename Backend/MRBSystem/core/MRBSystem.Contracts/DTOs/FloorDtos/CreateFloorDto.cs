namespace MRBSystem.Contracts.DTOs.FloorDtos;

public record CreateFloorDto
{
    public string Name { get; set; } = null!;
    public int LevelNumber { get; set; }
    public Guid BuildingId { get; set; }
}
