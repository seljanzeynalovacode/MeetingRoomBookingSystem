namespace MRBSystem.Contracts.DTOs.CalendarDtos;

public record RoomCalendarDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public DateTime Date { get; set; }
    public ICollection<CalendarReservationDto> Reservations { get; set; }
        = new List<CalendarReservationDto>();
    public ICollection<CalendarBlackoutDto> Blackouts { get; set; }
        = new List<CalendarBlackoutDto>();
}
