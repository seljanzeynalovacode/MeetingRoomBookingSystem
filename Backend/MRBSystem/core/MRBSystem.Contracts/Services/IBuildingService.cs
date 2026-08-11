using MRBSystem.Contracts.DTOs.BuildingDtos;

namespace MRBSystem.Contracts.Services;

public interface IBuildingService
{
    Task<ResponseBuildingDto> CreateAsync(CreateBuildingDto dto);
    Task<ResponseBuildingDto> UpdateAsync(Guid id, UpdateBuildingDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseBuildingDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseBuildingDto>> GetAllAsync();
}
