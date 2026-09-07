using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRBSystem.Contracts.DTOs.SystemSettingsDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class SystemSettingsService : ISystemSettingsService
{
    private readonly IGenericRepository<SystemSettings> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(
        IGenericRepository<SystemSettings> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SystemSettingsService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ResponseSystemSettingsDto> GetAsync(Guid? buildingId = null)
    {
        if (buildingId == null)
        {
            var global = await GetOrCreateGlobalEntityAsync();
            return _mapper.Map<ResponseSystemSettingsDto>(global);
        }

        var buildingEntity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(s => s.BuildingId == buildingId);

        if (buildingEntity != null)
        {
            return _mapper.Map<ResponseSystemSettingsDto>(buildingEntity);
        }

        // Binaya məxsus override yoxdursa, qlobal defolta fallback et
        var globalFallback = await GetOrCreateGlobalEntityAsync();
        return _mapper.Map<ResponseSystemSettingsDto>(globalFallback);
    }

    public async Task<ResponseSystemSettingsDto> UpdateAsync(Guid? buildingId, UpdateSystemSettingsDto dto)
    {
        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var existing = await _repository.GetQueryable()
                .FirstOrDefaultAsync(s => s.BuildingId == buildingId);

            if (existing == null)
            {
                existing = new SystemSettings { BuildingId = buildingId };
                await _repository.AddAsync(existing);
            }

            existing.MinReservationMinutes = dto.MinReservationMinutes;
            existing.MaxReservationHours = dto.MaxReservationHours;
            existing.BufferMinutes = dto.BufferMinutes;
            existing.BookingHorizonDays = dto.BookingHorizonDays;
            existing.IsActiveReservationLimitEnabled = dto.IsActiveReservationLimitEnabled;
            existing.MaxActiveReservationsPerUser = dto.MaxActiveReservationsPerUser;
            existing.MaxCalendarRangeDays = dto.MaxCalendarRangeDays;

            await _repository.UpdateAsync(existing);
            return existing;
        });

        _logger.LogInformation("System settings updated for building {BuildingId}.", buildingId?.ToString() ?? "GLOBAL");
        return _mapper.Map<ResponseSystemSettingsDto>(entity);
    }

    public async Task ResetToGlobalAsync(Guid buildingId)
    {
        var existing = await _repository.GetQueryable()
            .FirstOrDefaultAsync(s => s.BuildingId == buildingId);

        if (existing == null)
        {
            return;
        }

        await _repository.DeleteAsync(existing.Id);
        _logger.LogInformation("System settings override removed for building {BuildingId}, reverted to global.", buildingId);
    }

    private async Task<SystemSettings> GetOrCreateGlobalEntityAsync()
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _repository.GetQueryable().FirstOrDefaultAsync(s => s.BuildingId == null);
            if (entity != null) return entity;

            entity = new SystemSettings
            {
                BuildingId = null,
                MinReservationMinutes = 30,
                MaxReservationHours = 4,
                BufferMinutes = 0,
                BookingHorizonDays = 30,
                IsActiveReservationLimitEnabled = false,
                MaxActiveReservationsPerUser = 5,
                MaxCalendarRangeDays = 90
            };
            await _repository.AddAsync(entity);
            _logger.LogInformation("Default global system settings created.");
            return entity;
        });
    }
}