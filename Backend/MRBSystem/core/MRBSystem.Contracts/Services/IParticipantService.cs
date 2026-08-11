using MRBSystem.Contracts.DTOs.ParticipantDtos;

namespace MRBSystem.Contracts.Services;

public interface IParticipantService
{
    Task<ResponseParticipantDto> CreateAsync(CreateParticipantDto dto);
    Task<ResponseParticipantDto> UpdateAsync(Guid id, UpdateParticipantDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseParticipantDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseParticipantDto>> GetByReservationIdAsync(Guid reservationId);
}
