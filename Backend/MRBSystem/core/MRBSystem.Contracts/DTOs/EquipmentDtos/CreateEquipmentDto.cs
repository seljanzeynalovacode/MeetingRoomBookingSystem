using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.EquipmentDtos;

public record CreateEquipmentDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = null!;
}