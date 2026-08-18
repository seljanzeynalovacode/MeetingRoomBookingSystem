using MRBSystem.Contracts.DTOs.BaseDtos;

namespace MRBSystem.Contracts.DTOs.ReservationDtos;

public record ResponseReservationDto : BaseDto
{
    public string Title { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public Guid ReservedByUserId { get; set; }
    public string ReservedByUserName { get; set; } = null!;
}
