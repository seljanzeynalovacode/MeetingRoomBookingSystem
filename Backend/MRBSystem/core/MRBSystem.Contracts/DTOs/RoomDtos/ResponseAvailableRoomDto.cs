namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record ResponseAvailableRoomDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public int Capacity { get; set; }
    public string FloorName { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public ICollection<string> Equipment { get; set; } = new List<string>();
}
