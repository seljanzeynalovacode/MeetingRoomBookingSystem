using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.FloorDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Infrasturucture.Services;

public class FloorService : IFloorService
{
    private readonly IGenericRepository<Floor> _repository;
    private readonly IMapper _mapper;

    public FloorService(IGenericRepository<Floor> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ResponseFloorDto> CreateAsync(CreateFloorDto dto)
    {
        var entity = _mapper.Map<Floor>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(f => f.Building)
            .FirstAsync(f => f.Id == entity.Id);

        return _mapper.Map<ResponseFloorDto>(created);
    }

    public async Task<ResponseFloorDto> UpdateAsync(Guid id, UpdateFloorDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Floor with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(f => f.Building)
            .FirstAsync(f => f.Id == id);

        return _mapper.Map<ResponseFloorDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseFloorDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(f => f.Building)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NotFoundException($"Floor with id {id} not found.");

        return _mapper.Map<ResponseFloorDto>(entity);
    }

    public async Task<ICollection<ResponseFloorDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(f => f.Building)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseFloorDto>>(entities);
    }
}
