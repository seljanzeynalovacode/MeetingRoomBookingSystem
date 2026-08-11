using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.RoomEquipmentDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class RoomEquipmentService : IRoomEquipmentService
{
    private readonly IGenericRepository<RoomEquipment> _repository;
    private readonly IMapper _mapper;

    public RoomEquipmentService(IGenericRepository<RoomEquipment> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    public async Task<ResponseRoomEquipmentDto> CreateAsync(CreateRoomEquipmentDto dto)
    {
        var entity = _mapper.Map<RoomEquipment>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(re => re.Equipment)
            .FirstAsync(re => re.Id == entity.Id);

        return _mapper.Map<ResponseRoomEquipmentDto>(created);
    }
    public async Task<ResponseRoomEquipmentDto> UpdateAsync(Guid id, UpdateRoomEquipmentDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"RoomEquipment with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(re => re.Equipment)
            .FirstAsync(re => re.Id == id);

        return _mapper.Map<ResponseRoomEquipmentDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseRoomEquipmentDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(re => re.Equipment)
            .FirstOrDefaultAsync(re => re.Id == id)
            ?? throw new NotFoundException($"RoomEquipment with id {id} not found.");

        return _mapper.Map<ResponseRoomEquipmentDto>(entity);
    }
    public async Task<ICollection<ResponseRoomEquipmentDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(re => re.Equipment)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseRoomEquipmentDto>>(entities);
    }
}
