using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.FloorDtos;

public record CreateFloorDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Range(-5, 500)]
    public int LevelNumber { get; set; }

    public Guid BuildingId { get; set; }
}