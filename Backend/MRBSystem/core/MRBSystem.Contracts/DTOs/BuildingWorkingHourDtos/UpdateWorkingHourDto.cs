using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;

public class UpdateWorkingHourDto : IValidatableObject
{
    [Required, EnumDataType(typeof(DayOfWeek))]
    public string DayOfWeek { get; set; } = null!;

    public bool IsWorkingDay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsWorkingDay && StartTime >= EndTime)
        {
            yield return new ValidationResult(
                "StartTime EndTime-dan əvvəl olmalıdır.",
                new[] { nameof(StartTime), nameof(EndTime) });
        }
    }
}
