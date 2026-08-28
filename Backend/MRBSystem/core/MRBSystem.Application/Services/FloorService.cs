using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Contracts.DTOs.FloorDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class FloorService : IFloorService
{
    private readonly IGenericRepository<Floor> _floorrepository;
    private readonly IGenericRepository<Building> _buildingRepository;
    private readonly IMapper _mapper;

    public FloorService(
        IGenericRepository<Floor> repository,
        IGenericRepository<Building> buildingRepository,
        IMapper mapper)
    {
        _floorrepository = repository;
        _buildingRepository = buildingRepository;
        _mapper = mapper;
    }

    public async Task<ResponseFloorDto> CreateAsync(CreateFloorDto dto)
    {
        var building = await _buildingRepository.GetByIdAsync(dto.BuildingId)
            ?? throw new NotFoundException($"Building with id {dto.BuildingId} not found.");

        var entity = _mapper.Map<Floor>(dto);
        await _floorrepository.AddAsync(entity);

        var created = await _floorrepository.GetQueryable()
            .Include(f => f.Building)
            .FirstAsync(f => f.Id == entity.Id);

        return _mapper.Map<ResponseFloorDto>(created);
    }

    public async Task<ResponseFloorDto> UpdateAsync(Guid id, UpdateFloorDto dto)
    {
        var entity = await _floorrepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Floor with id {id} not found.");

        var building = await _buildingRepository.GetByIdAsync(dto.BuildingId)
            ?? throw new NotFoundException($"Building with id {dto.BuildingId} not found.");

        _mapper.Map(dto, entity);
        await _floorrepository.UpdateAsync(entity);

        var updated = await _floorrepository.GetQueryable()
            .Include(f => f.Building)
            .FirstAsync(f => f.Id == id);

        return _mapper.Map<ResponseFloorDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _floorrepository.DeleteAsync(id);
    }

    public async Task<ResponseFloorDto> GetByIdAsync(Guid id)
    {
        var entity = await _floorrepository.GetQueryable()
            .Include(f => f.Building)
            .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NotFoundException($"Floor with id {id} not found.");

        return _mapper.Map<ResponseFloorDto>(entity);
    }

    public async Task<ICollection<ResponseFloorDto>> GetAllAsync()
    {
        var entities = await _floorrepository.GetQueryable()
            .Include(f => f.Building)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseFloorDto>>(entities);
    }
}