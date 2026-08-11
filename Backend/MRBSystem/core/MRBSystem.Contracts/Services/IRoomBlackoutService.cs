using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;

namespace MRBSystem.Contracts.Services;

public interface IRoomBlackoutService
{
    Task<ResponseRoomBlackoutDto> CreateAsync(CreateRoomBlackoutDto dto);
    Task<ResponseRoomBlackoutDto> UpdateAsync(Guid id, UpdateRoomBlackoutDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseRoomBlackoutDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseRoomBlackoutDto>> GetAllAsync();
}
