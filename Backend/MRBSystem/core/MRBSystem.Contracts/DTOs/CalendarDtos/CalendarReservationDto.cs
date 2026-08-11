namespace MRBSystem.Contracts.DTOs.CalendarDtos;

public record CalendarReservationDto
{
    public Guid ReservationId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string OrganizerName { get; set; } = null!;
    public string Status { get; set; } = null!;
}
