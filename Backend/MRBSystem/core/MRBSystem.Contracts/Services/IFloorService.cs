using MRBSystem.Contracts.DTOs.FloorDtos;

namespace MRBSystem.Contracts.Services;

public interface IFloorService
{
    Task<ResponseFloorDto> CreateAsync(CreateFloorDto dto);
    Task<ResponseFloorDto> UpdateAsync(Guid id, UpdateFloorDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseFloorDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseFloorDto>> GetAllAsync();

}
