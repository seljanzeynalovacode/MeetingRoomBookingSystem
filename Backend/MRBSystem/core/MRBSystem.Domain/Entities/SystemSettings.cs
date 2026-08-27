namespace MRBSystem.Domain.Entities;

public class SystemSettings : BaseEntity
{
    public int MinReservationMinutes { get; set; }
    public int MaxReservationHours { get; set; }
    public int BufferMinutes { get; set; }
    public int BookingHorizonDays { get; set; }
    public bool IsActiveReservationLimitEnabled { get; set; }
    public int MaxActiveReservationsPerUser { get; set; }
    public int MaxCalendarRangeDays { get; set; }
}