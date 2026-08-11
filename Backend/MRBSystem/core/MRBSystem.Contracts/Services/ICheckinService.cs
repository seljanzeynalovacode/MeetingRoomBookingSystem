using MRBSystem.Contracts.DTOs.CheckinDtos;

namespace MRBSystem.Contracts.Services;

public interface ICheckinService
{
    Task<ResponseCheckinDto> CheckInAsync(CreateCheckinDto dto);
    Task<ResponseCheckinDto> GetByIdAsync(Guid id);
}
