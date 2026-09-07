using MRBSystem.Contracts.DTOs.SystemSettingsDtos;

namespace MRBSystem.Contracts.Services;

public interface ISystemSettingsService
{
    Task<ResponseSystemSettingsDto> GetAsync(Guid? buildingId = null);
    Task<ResponseSystemSettingsDto> UpdateAsync(Guid? buildingId, UpdateSystemSettingsDto dto);
    Task ResetToGlobalAsync(Guid buildingId);
}
