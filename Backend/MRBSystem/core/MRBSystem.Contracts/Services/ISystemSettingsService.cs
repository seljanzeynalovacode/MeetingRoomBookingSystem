using MRBSystem.Contracts.DTOs.SystemSettingsDtos;

namespace MRBSystem.Contracts.Services;

public interface ISystemSettingsService
{
    Task<ResponseSystemSettingsDto> GetAsync();
    Task<ResponseSystemSettingsDto> UpdateAsync(UpdateSystemSettingsDto dto);
}
