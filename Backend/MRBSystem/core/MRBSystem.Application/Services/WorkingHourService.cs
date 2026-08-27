using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using System.Data;

namespace MRBSystem.Application.Services;

public class WorkingHourService : IWorkingHourService
{
    private static readonly DayOfWeek[] WeekOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    private readonly IGenericRepository<BuildingWorkingHour> _repository;
    private readonly IGenericRepository<Building> _buildingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<WorkingHourService> _logger;

    public WorkingHourService(
        IGenericRepository<BuildingWorkingHour> repository,
        IGenericRepository<Building> buildingRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WorkingHourService> logger)
    {
        _repository = repository;
        _buildingRepository = buildingRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ICollection<ResponseWorkingHourDto>> GetByBuildingIdAsync(Guid buildingId)
    {
        var building = await _buildingRepository.GetByIdAsync(buildingId)
     ?? throw new NotFoundException($"Building with id {buildingId} not found.");

        var rows = await _repository.GetQueryable()
            .Where(w => w.BuildingId == buildingId)
            .ToListAsync();

        if (rows.Count == 0)
        {
            await SeedDefaultsAsync(buildingId);
            rows = await _repository.GetQueryable()
                .Where(w => w.BuildingId == buildingId)
                .ToListAsync();
        }

        return _mapper.Map<ICollection<ResponseWorkingHourDto>>(OrderByWeekday(rows));
    }

    public async Task<ICollection<ResponseWorkingHourDto>> UpdateForBuildingAsync(Guid buildingId, List<UpdateWorkingHourDto> dto)
    {
        var building = await _buildingRepository.GetByIdAsync(buildingId)
    ?? throw new NotFoundException($"Building with id {buildingId} not found.");

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var rows = await _repository.GetQueryable()
                .Where(w => w.BuildingId == buildingId)
                .ToListAsync();

            if (rows.Count == 0)
            {
                await SeedDefaultsAsync(buildingId);
                rows = await _repository.GetQueryable()
                    .Where(w => w.BuildingId == buildingId)
                    .ToListAsync();
            }

            foreach (var item in dto)
            {
                var day = Enum.Parse<DayOfWeek>(item.DayOfWeek, ignoreCase: true);
                var row = rows.FirstOrDefault(w => w.DayOfWeek == day)
                    ?? throw new NotFoundException($"Working hour row for {item.DayOfWeek} not found.");

                row.IsWorkingDay = item.IsWorkingDay;
                row.StartTime = item.StartTime;
                row.EndTime = item.EndTime;
                await _repository.UpdateAsync(row);
            }

            _logger.LogInformation("Working hours updated for building {BuildingId}", buildingId);

            var updated = await _repository.GetQueryable()
                .Where(w => w.BuildingId == buildingId)
                .ToListAsync();

            return _mapper.Map<ICollection<ResponseWorkingHourDto>>(OrderByWeekday(updated));
        }, IsolationLevel.ReadCommitted);
    }

    public async Task SeedDefaultsAsync(Guid buildingId)
    {
        foreach (var day in WeekOrder)
        {
            var isWorkingDay = day != DayOfWeek.Saturday && day != DayOfWeek.Sunday;
            await _repository.AddAsync(new BuildingWorkingHour
            {
                BuildingId = buildingId,
                DayOfWeek = day,
                IsWorkingDay = isWorkingDay,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0)
            });
        }

        _logger.LogInformation("Default working hours seeded for building {BuildingId}", buildingId);
    }

    private static IEnumerable<BuildingWorkingHour> OrderByWeekday(IEnumerable<BuildingWorkingHour> rows)
        => rows.OrderBy(r => Array.IndexOf(WeekOrder, r.DayOfWeek));
}
