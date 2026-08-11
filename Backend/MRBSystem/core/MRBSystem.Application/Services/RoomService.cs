using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Infrasturucture.Services;

public class RoomService : IRoomService
{
    private readonly IGenericRepository<Room> _repository;
    private readonly IMapper _mapper;

    public RoomService(IGenericRepository<Room> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ResponseRoomDto> CreateAsync(CreateRoomDto dto)
    {
        var entity = _mapper.Map<Room>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstAsync(r => r.Id == entity.Id);

        return _mapper.Map<ResponseRoomDto>(created);
    }

    public async Task<ResponseRoomDto> UpdateAsync(Guid id, UpdateRoomDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Room with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstAsync(r => r.Id == id);

        return _mapper.Map<ResponseRoomDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseRoomDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Room with id {id} not found.");

        return _mapper.Map<ResponseRoomDto>(entity);
    }

    public async Task<ICollection<ResponseRoomDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseRoomDto>>(entities);
    }
}
