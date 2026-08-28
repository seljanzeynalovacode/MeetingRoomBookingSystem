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

    public async Task<ResponseSystemSettingsDto> GetAsync()
    {
        var entity = await GetOrCreateEntityAsync();
        return _mapper.Map<ResponseSystemSettingsDto>(entity);
    }

    public async Task<ResponseSystemSettingsDto> UpdateAsync(UpdateSystemSettingsDto dto)
    {
        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var existing = await _repository.GetQueryable().FirstOrDefaultAsync();

            if (existing == null)
            {
                var created = new SystemSettings
                {
                    MinReservationMinutes = dto.MinReservationMinutes,
                    MaxReservationHours = dto.MaxReservationHours,
                    BufferMinutes = dto.BufferMinutes,
                    BookingHorizonDays = dto.BookingHorizonDays,
                    IsActiveReservationLimitEnabled = dto.IsActiveReservationLimitEnabled,
                    MaxActiveReservationsPerUser = dto.MaxActiveReservationsPerUser,
                    MaxCalendarRangeDays = dto.MaxCalendarRangeDays
                };
                await _repository.AddAsync(created);
                return created;
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

        _logger.LogInformation("System settings updated.");
        return _mapper.Map<ResponseSystemSettingsDto>(entity);
    }

    private async Task<SystemSettings> GetOrCreateEntityAsync()
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _repository.GetQueryable().FirstOrDefaultAsync();
            if (entity != null) return entity;

            entity = new SystemSettings
            {
                MinReservationMinutes = 30,
                MaxReservationHours = 4,
                BufferMinutes = 0,
                BookingHorizonDays = 30,
                IsActiveReservationLimitEnabled = false,
                MaxActiveReservationsPerUser = 5,
                MaxCalendarRangeDays = 90
            };
            await _repository.AddAsync(entity);
            _logger.LogInformation("Default system settings created.");
            return entity;
        });
    }
}