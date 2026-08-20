using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.RoomEquipmentDtos;

public record ResponseRoomEquipmentDto : BaseDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public int Quantity { get; set; }
}
