using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.SystemSettingsDtos;

public record UpdateSystemSettingsDto : IValidatableObject
{
    [Range(5, 240)]
    public int MinReservationMinutes { get; set; }

    [Range(1, 24)]
    public int MaxReservationHours { get; set; }

    [Range(0, 120)]
    public int BufferMinutes { get; set; }

    [Range(1, 365)]
    public int BookingHorizonDays { get; set; }

    public bool IsActiveReservationLimitEnabled { get; set; }

    [Range(1, 100)]
    public int MaxActiveReservationsPerUser { get; set; }

    [Range(7, 365)]
    public int MaxCalendarRangeDays { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinReservationMinutes >= MaxReservationHours * 60)
        {
            yield return new ValidationResult(
                "Minimum müddət maksimum müddətdən az olmalıdır.",
                new[] { nameof(MinReservationMinutes), nameof(MaxReservationHours) });
        }
    }
}