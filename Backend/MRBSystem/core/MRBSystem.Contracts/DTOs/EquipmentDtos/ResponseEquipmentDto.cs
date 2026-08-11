using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.EquipmentDtos;

public record ResponseEquipmentDto : BaseDto
{
    public string Name { get; set; } = null!;
}
