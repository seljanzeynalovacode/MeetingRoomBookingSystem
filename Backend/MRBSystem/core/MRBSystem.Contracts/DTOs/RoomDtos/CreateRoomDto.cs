using System.ComponentModel.DataAnnotations;

namespace MRBSystem.Contracts.DTOs.RoomDtos;

public record CreateRoomDto
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [Range(1, 1000)]
    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid FloorId { get; set; }
}
