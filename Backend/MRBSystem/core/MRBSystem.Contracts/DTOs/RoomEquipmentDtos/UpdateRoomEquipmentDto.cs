using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.RoomEquipmentDtos;

public record UpdateRoomEquipmentDto
{
    [Range(1, 1000)]
    public int Quantity { get; set; }
}