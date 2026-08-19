using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.EquipmentDtos;

public record UpdateEquipmentDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = null!;
}