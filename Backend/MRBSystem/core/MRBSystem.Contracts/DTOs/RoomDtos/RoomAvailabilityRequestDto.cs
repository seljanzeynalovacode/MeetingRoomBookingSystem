using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record RoomAvailabilityRequestDto : IValidatableObject
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid BuildingId { get; set; }
    public Guid? FloorId { get; set; }
    public List<Guid>? EquipmentIds { get; set; }

    [Range(1, 1000)]
    public int Capacity { get; set; }

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