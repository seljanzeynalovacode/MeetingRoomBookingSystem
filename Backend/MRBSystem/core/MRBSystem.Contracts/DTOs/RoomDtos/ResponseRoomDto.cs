using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record ResponseRoomDto : BaseDto
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public Guid FloorId { get; set; }
    public string FloorName { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
}
