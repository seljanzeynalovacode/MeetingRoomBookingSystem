using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.CheckinDtos;

public record ResponseCheckinDto :  BaseDto
{
    public Guid ReservationId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public DateTime CheckinTime { get; set; }
    public DateTime? CheckoutTime { get; set; }
}
