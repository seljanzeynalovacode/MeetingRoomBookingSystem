namespace MRBSystem.Domain.Entities;

public class BuildingWorkingHour : BaseEntity
{
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
