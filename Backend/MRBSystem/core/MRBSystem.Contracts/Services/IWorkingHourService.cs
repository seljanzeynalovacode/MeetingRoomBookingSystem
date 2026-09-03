using MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;

namespace MRBSystem.Contracts.Services;

public interface IWorkingHourService
{
    Task<ICollection<ResponseWorkingHourDto>> GetByBuildingIdAsync(Guid buildingId);
    Task<ICollection<ResponseWorkingHourDto>> UpdateForBuildingAsync(Guid buildingId, List<UpdateWorkingHourDto> dto);
    Task SeedDefaultsAsync(Guid buildingId);
    Task EnsureWithinWorkingHoursAsync(Guid buildingId, DateTime localStart, DateTime localEnd);
}
