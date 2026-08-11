using AutoMapper;
using MRBSystem.Contracts.DTOs.EquipmentDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Infrasturucture.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IGenericRepository<Equipment> _repository;
    private readonly IMapper _mapper;

    public EquipmentService(IGenericRepository<Equipment> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    public async Task<ResponseEquipmentDto> CreateAsync(CreateEquipmentDto dto)
    {
        var entity = _mapper.Map<Equipment>(dto);
        await _repository.AddAsync(entity);
        return _mapper.Map<ResponseEquipmentDto>(entity);
    }
    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
    public async Task<ResponseEquipmentDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"Equipment with id {id} not found.");
        }
        return _mapper.Map<ResponseEquipmentDto>(entity);
    }
    public async Task<ICollection<ResponseEquipmentDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return _mapper.Map<ICollection<ResponseEquipmentDto>>(entities);
    }
    public async Task<ResponseEquipmentDto> UpdateAsync(Guid id, UpdateEquipmentDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception($"Equipment with id {id} not found.");
        }
        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        return _mapper.Map<ResponseEquipmentDto>(entity);

    }
}
