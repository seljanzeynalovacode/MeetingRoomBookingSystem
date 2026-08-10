namespace MRBSystem.Contracts.DTOs.RoomBlackoutDtos;

public record ResponseRoomBlackoutDto : BaseDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = null!;
}
