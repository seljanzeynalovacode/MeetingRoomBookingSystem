namespace MRBSystem.Contracts.DTOs.RoomEquipmentDtos;

public record CreateRoomEquipmentDto
{
    public Guid RoomId { get; set; }
    public Guid EquipmentId { get; set; }
    public int Quantity { get; set; } = 1;
}
