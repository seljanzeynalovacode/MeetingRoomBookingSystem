namespace MRBSystem.Contracts.DTOs.SystemSettingsDtos;

public record ResponseSystemSettingsDto
{
    public Guid Id { get; set; }
    public Guid? BuildingId { get; set; }
    public int MinReservationMinutes { get; set; }
    public int MaxReservationHours { get; set; }
    public int BufferMinutes { get; set; }
    public int BookingHorizonDays { get; set; }
    public bool IsActiveReservationLimitEnabled { get; set; }
    public int MaxActiveReservationsPerUser { get; set; }
    public int MaxCalendarRangeDays { get; set; }
}