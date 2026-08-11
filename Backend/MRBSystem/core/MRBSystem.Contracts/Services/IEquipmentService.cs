using MRBSystem.Contracts.DTOs.EquipmentDtos;

namespace MRBSystem.Contracts.Services;

public interface IEquipmentService
{
    Task<ResponseEquipmentDto> CreateAsync(CreateEquipmentDto dto);
    Task<ResponseEquipmentDto> UpdateAsync(Guid id, UpdateEquipmentDto dto);
    Task DeleteAsync(Guid id);
    Task<ResponseEquipmentDto> GetByIdAsync(Guid id);
    Task<ICollection<ResponseEquipmentDto>> GetAllAsync();
}
