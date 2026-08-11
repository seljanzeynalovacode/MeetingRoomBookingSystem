using MRBSystem.Contracts.DTOs.RoomEquipmentDtos;

namespace MRBSystem.Contracts.Services;

public interface IRoomEquipmentService
{
    Task<ResponseRoomEquipmentDto> CreateAsync(CreateRoomEquipmentDto dto);
    Task<ResponseRoomEquipmentDto> UpdateAsync(Guid id, UpdateRoomEquipmentDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseRoomEquipmentDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseRoomEquipmentDto>> GetAllAsync();


}
