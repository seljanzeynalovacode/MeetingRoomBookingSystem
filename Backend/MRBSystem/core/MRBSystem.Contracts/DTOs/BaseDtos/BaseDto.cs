namespace MRBSystem.Contracts.DTOs.BaseDtos;

public record BaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
