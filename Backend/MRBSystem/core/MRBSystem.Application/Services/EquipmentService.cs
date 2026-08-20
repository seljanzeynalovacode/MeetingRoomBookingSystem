using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.EquipmentDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IGenericRepository<Equipment> _repository;
    private readonly IGenericRepository<RoomEquipment> _roomEquipmentRepository;
    private readonly IMapper _mapper;

    public EquipmentService(IGenericRepository<Equipment> repository, IGenericRepository<RoomEquipment> roomEquipmentRepository, IMapper mapper)
    {
        _repository = repository;
        _roomEquipmentRepository = roomEquipmentRepository;
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
        var equipment = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Equipment with id {id} not found.");

        var isInUse = await _roomEquipmentRepository.GetQueryable()
            .AnyAsync(re => re.EquipmentId == id);

        if (isInUse)
        {
            throw new InvalidOperationException("Bu avadanlıq otaqlara təyin edilib, silinə bilməz.");
        }

        await _repository.DeleteAsync(id);
    }
    public async Task<ResponseEquipmentDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Equipment with id {id} not found.");

        return _mapper.Map<ResponseEquipmentDto>(entity);
    }
    public async Task<ICollection<ResponseEquipmentDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return _mapper.Map<ICollection<ResponseEquipmentDto>>(entities);
    }
    public async Task<ResponseEquipmentDto> UpdateAsync(Guid id, UpdateEquipmentDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Equipment with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        return _mapper.Map<ResponseEquipmentDto>(entity);
    }
}