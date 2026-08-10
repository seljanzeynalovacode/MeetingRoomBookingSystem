namespace MRBSystem.Contracts.DTOs.RoomBlackoutDtos;

public record UpdateRoomBlackoutDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = null!;
}
