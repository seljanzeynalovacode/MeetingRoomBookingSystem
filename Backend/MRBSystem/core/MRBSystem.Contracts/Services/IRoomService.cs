using MRBSystem.Contracts.DTOs.CalendarDtos;
using MRBSystem.Contracts.DTOs.RoomDtos;

namespace MRBSystem.Contracts.Services;

public interface IRoomService
{
    Task<ResponseRoomDto> CreateAsync(CreateRoomDto dto);
    Task<ResponseRoomDto> UpdateAsync(Guid id, UpdateRoomDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseRoomDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseRoomDto>> GetAllAsync();
    Task<ICollection<ResponseAvailableRoomDto>> SearchAvailableAsync(RoomAvailabilityRequestDto dto);
    Task<ICollection<RoomCalendarDto>> GetRoomCalendarAsync(Guid roomId, DateTime startDate, DateTime endDate);
}
