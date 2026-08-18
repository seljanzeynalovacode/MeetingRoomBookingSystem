using MRBSystem.Contracts.DTOs.ReservationDtos;

namespace MRBSystem.Contracts.Services;

public interface IReservationService
{
    Task<ResponseReservationDto> CreateAsync(CreateReservationDto dto);
    Task<ResponseReservationDto> UpdateAsync(Guid id, UpdateReservationDto dto);
    Task CancelAsync(Guid id);
    Task<ResponseReservationDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseReservationDto>> GetAllAsync();
    Task<ICollection<ResponseReservationDto>> GetMyReservationsAsync();

    Task<ICollection<ResponseReservationDto>> GetByUserIdAsync(Guid userId);
}
