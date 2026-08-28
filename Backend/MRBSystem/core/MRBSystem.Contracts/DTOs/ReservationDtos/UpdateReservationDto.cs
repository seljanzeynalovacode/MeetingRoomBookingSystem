using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.ReservationDtos;

public record UpdateReservationDto : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid RoomId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate >= EndDate)
        {
            yield return new ValidationResult(
                "StartDate EndDate-dən əvvəl olmalıdır.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }

        if (StartDate < DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "Keçmiş tarix və saata rezervasiya edilə bilməz.",
                new[] { nameof(StartDate) });
        }

    }
}