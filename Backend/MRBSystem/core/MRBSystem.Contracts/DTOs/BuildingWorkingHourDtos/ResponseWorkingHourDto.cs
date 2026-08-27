namespace MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;

public class ResponseWorkingHourDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string DayOfWeek { get; set; } = null!;
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
