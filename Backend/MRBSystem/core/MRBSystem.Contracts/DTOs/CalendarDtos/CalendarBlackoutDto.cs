namespace MRBSystem.Contracts.DTOs.CalendarDtos;

public record CalendarBlackoutDto
{
    public Guid BlackoutId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = null!;
}
