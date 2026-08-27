using AutoMapper;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.BuildingDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class BuildingService : IBuildingService
{
    private readonly IGenericRepository<Building> _repository;
    private readonly IWorkingHourService _workingHourService;
    private readonly IMapper _mapper;

    public BuildingService(IGenericRepository<Building> repository, IWorkingHourService workingHourService, IMapper mapper)
    {
        _repository = repository;
        _workingHourService = workingHourService;
        _mapper = mapper;
    }

    public async Task<ResponseBuildingDto> CreateAsync(CreateBuildingDto dto)
    {
        var entity = _mapper.Map<Building>(dto);
        await _repository.AddAsync(entity);
        await _workingHourService.SeedDefaultsAsync(entity.Id);
        return _mapper.Map<ResponseBuildingDto>(entity);
    }

    public async Task<ResponseBuildingDto> UpdateAsync(Guid id, UpdateBuildingDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Building with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        return _mapper.Map<ResponseBuildingDto>(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseBuildingDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Building with id {id} not found.");

        return _mapper.Map<ResponseBuildingDto>(entity);
    }

    public async Task<ICollection<ResponseBuildingDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return _mapper.Map<ICollection<ResponseBuildingDto>>(entities);
    }
}
