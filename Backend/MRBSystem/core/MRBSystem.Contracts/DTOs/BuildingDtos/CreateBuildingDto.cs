using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.BuildingDtos;

public record CreateBuildingDto
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = null!;

    [Required, StringLength(300, MinimumLength = 3)]
    public string Address { get; set; } = null!;
}
