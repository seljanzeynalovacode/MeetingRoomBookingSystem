using System.ComponentModel.DataAnnotations;
using MRBSystem.Domain.Enums;

namespace MRBSystem.Contracts.DTOs.ParticipantDtos;

public record UpdateParticipantDto
{
    [Required, EnumDataType(typeof(ResponseStatus))]
    public string ResponseStatus { get; set; } = null!;

    [EmailAddress, StringLength(256)]
    public string? GuestEmail { get; set; }

    [StringLength(200, MinimumLength = 2)]
    public string? GuestName { get; set; }
}