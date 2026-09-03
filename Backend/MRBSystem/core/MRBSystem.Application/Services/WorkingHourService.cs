using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRBSystem.Shared.Exceptions;
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
            rows = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var existing = await _repository.GetQueryable()
                    .Where(w => w.BuildingId == buildingId)
                    .ToListAsync();
                if (existing.Count > 0) return existing;

                await SeedDefaultsAsync(buildingId);
                return await _repository.GetQueryable()
                    .Where(w => w.BuildingId == buildingId)
                    .ToListAsync();
            });
        }

        return _mapper.Map<ICollection<ResponseWorkingHourDto>>(OrderByWeekday(rows));
    }

    public async Task<ICollection<ResponseWorkingHourDto>> UpdateForBuildingAsync(Guid buildingId, List<UpdateWorkingHourDto> dto)
    {
        var building = await _buildingRepository.GetByIdAsync(buildingId)
    ?? throw new NotFoundException($"Building with id {buildingId} not found.");

        var duplicateDays = dto
            .GroupBy(item => Enum.Parse<DayOfWeek>(item.DayOfWeek, ignoreCase: true))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateDays.Count > 0)
        {
            throw new ArgumentException(
                $"Eyni gün üçün birdən çox qeyd göndərilib: {string.Join(", ", duplicateDays)}");
        }

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

    public async Task EnsureWithinWorkingHoursAsync(Guid buildingId, DateTime localStart, DateTime localEnd)
    {
        var workingHours = await GetByBuildingIdAsync(buildingId);
        var todayHours = workingHours.FirstOrDefault(h => h.DayOfWeek == localStart.DayOfWeek.ToString());

        if (todayHours == null || !todayHours.IsWorkingDay)
        {
            throw new ArgumentException("Seçilmiş tarixdə bina iş günü deyil.");
        }

        var startOffset = localStart.TimeOfDay;
        var endOffset = localEnd - localStart.Date;

        if (startOffset < todayHours.StartTime || endOffset > todayHours.EndTime)
        {
            var startText = todayHours.StartTime.ToString(@"hh\:mm");
            var endText = todayHours.EndTime.ToString(@"hh\:mm");
            throw new ArgumentException(
                $"Seçilmiş saat iş saatları xaricindədir. Zəhmət olmasa {startText}-{endText} aralığında bir saat təyin edin.");
        }
    }

    private static IEnumerable<BuildingWorkingHour> OrderByWeekday(IEnumerable<BuildingWorkingHour> rows)
        => rows.OrderBy(r => Array.IndexOf(WeekOrder, r.DayOfWeek));
}