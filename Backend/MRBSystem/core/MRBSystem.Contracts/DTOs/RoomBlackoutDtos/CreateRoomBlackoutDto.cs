namespace MRBSystem.Contracts.DTOs.RoomBlackoutDtos;

public record CreateRoomBlackoutDto
{
    public Guid RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = null!;
}
