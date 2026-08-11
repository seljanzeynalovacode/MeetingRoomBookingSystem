namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record RoomAvailabilityRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? FloorId { get; set; }
    public List<Guid>? EquipmentIds { get; set; }
    public int MinimumCapacity { get; set; }
}
