namespace MRBSystem.Contracts.DTOs.ReservationDtos;

public record UpdateReservationDto
{
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid RoomId { get; set; }
    public Guid OrganizerId { get; set; }
    public DateTime CheckinDeadline { get; set; }
}
