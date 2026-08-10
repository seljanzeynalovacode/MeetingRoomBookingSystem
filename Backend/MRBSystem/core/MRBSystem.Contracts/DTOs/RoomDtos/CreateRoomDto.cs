namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record CreateRoomDto
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid FloorId { get; set; }
}
