using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.RoomBlackoutDtos;

public record UpdateRoomBlackoutDto : IValidatableObject
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate >= EndDate)
        {
            yield return new ValidationResult(
                "StartDate EndDate-dən əvvəl olmalıdır.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
}