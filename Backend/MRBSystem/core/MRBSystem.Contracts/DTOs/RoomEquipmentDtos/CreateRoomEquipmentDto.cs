using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.RoomEquipmentDtos;

public record CreateRoomEquipmentDto
{
    public Guid RoomId { get; set; }
    public Guid EquipmentId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}