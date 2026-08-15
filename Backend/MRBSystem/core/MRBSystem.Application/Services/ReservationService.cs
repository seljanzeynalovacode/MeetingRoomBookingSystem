using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.ReservationDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class ReservationService : IReservationService
{
    private readonly IGenericRepository<Reservation> _repository;
    private readonly IGenericRepository<RoomBlackout> _blackoutRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IGenericRepository<Reservation> repository,
        IGenericRepository<RoomBlackout> blackoutRepository,
        ICurrentUserService currentUser,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<ReservationService> logger)
    {
        _repository = repository;
        _blackoutRepository = blackoutRepository;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ResponseReservationDto> CreateAsync(CreateReservationDto dto)
    {
        await ValidateDatesAsync(dto.RoomId, dto.StartDate, dto.EndDate);

        var entity = _mapper.Map<Reservation>(dto);
        entity.ReservedByUserId = _currentUser.UserId;
        entity.QrCodeToken = Guid.NewGuid().ToString();
        entity.CheckinDeadline = dto.StartDate.AddMinutes(15); // ne zaman autocancel olsun

        await _repository.AddAsync(entity);

        _logger.LogInformation(
            "Reservation {ReservationId} created by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, dto.RoomId);

        var created = await GetWithDetailsAsync().FirstAsync(r => r.Id == entity.Id);

        await _notificationService.CreateAndSendAsync(created, NotificationType.Created);
        _logger.LogDebug(
            "Notification sent for created reservation {ReservationId} to relevant users",
            created.Id);

        return _mapper.Map<ResponseReservationDto>(created);
    }

    public async Task<ResponseReservationDto> UpdateAsync(Guid id, UpdateReservationDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            _logger.LogWarning("Reservation with id {ReservationId} not found for update", id);
            throw new NotFoundException($"Reservation with id {id} not found.");
        }

        await ValidateDatesAsync(dto.RoomId, dto.StartDate, dto.EndDate, excludeReservationId: id);

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        _logger.LogInformation(
            "Reservation {ReservationId} updated by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, dto.RoomId);

        var updated = await GetWithDetailsAsync().FirstAsync(r => r.Id == id);

        await _notificationService.CreateAndSendAsync(updated, NotificationType.Updated);
        _logger.LogDebug(
            "Notification sent for updated reservation {ReservationId} to relevant users",
            updated.Id);

        return _mapper.Map<ResponseReservationDto>(updated);
    }

    public async Task CancelAsync(Guid id)
    {
        var entity = await GetWithDetailsAsync().FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null){
            _logger.LogWarning("Reservation with id {ReservationId} not found for cancellation", id);
            throw new NotFoundException($"Reservation with id {id} not found.");
        }
        if (entity.Status == ReservationStatus.Cancelled || entity.Status == ReservationStatus.AutoCancelled)
        {
            _logger.LogWarning("Attempted to cancel an already cancelled reservation {ReservationId}", id);
            // Layihəndəki xəta siniflərinə uyğun olaraq BadRequestException və ya InvalidOperationException ata bilərsən
            throw new InvalidOperationException("Bu rezervasiya artıq ləğv edilib!");
        }
        entity.Status = ReservationStatus.Cancelled;
        await _repository.UpdateAsync(entity);
        _logger.LogInformation(
            "Reservation {ReservationId} cancelled by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, entity.RoomId);

        await _notificationService.CreateAndSendAsync(entity, NotificationType.Cancelled);
    }

    public async Task<ResponseReservationDto> GetByIdAsync(Guid id)
    {
        var entity = await GetWithDetailsAsync().FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null) 
        {
            _logger.LogWarning("Reservation with id {ReservationId} not found", id);
            throw new NotFoundException($"Reservation with id {id} not found.");
        }
        _logger.LogDebug("Fetched reservation {ReservationId} with related entities", id);
        return _mapper.Map<ResponseReservationDto>(entity);
    }

    public async Task<ICollection<ResponseReservationDto>> GetAllAsync()
    {
        var entities = await GetWithDetailsAsync().ToListAsync();
        return _mapper.Map<ICollection<ResponseReservationDto>>(entities);
    }

    private IQueryable<Reservation> GetWithDetailsAsync()
    {
       
        return _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser)
            .Include(r => r.Organizer)
            .Include(r => r.CheckIn);
    }

    private async Task ValidateDatesAsync(Guid roomId, DateTime startDate, DateTime endDate, Guid? excludeReservationId = null)
    {
        if (startDate >= endDate)
        {
            _logger.LogWarning(
                "Invalid date range for room {RoomId}: StartDate {StartDate} is not earlier than EndDate {EndDate}",
                roomId, startDate, endDate);
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var hasConflict = await _repository.GetQueryable()
            .AnyAsync(r => r.Id != excludeReservationId
                && r.RoomId == roomId
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.AutoCancelled
                && startDate < r.EndDate
                && endDate > r.StartDate);

        if (hasConflict)
        {
            _logger.LogWarning(
                "Attempt to create/update reservation for room {RoomId} with conflicting time range {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new InvalidOperationException("Room is already booked for the selected time range.");
        }
        var hasBlackout = await _blackoutRepository.GetQueryable()
            .AnyAsync(b => b.RoomId == roomId
                && startDate < b.EndDate
                && endDate > b.StartDate);

        if (hasBlackout)
        {
            _logger.LogWarning(
                "Attempt to create/update reservation for room {RoomId} during blackout time range {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new InvalidOperationException("Room is blacked out for the selected time range.");
        }
    }
}
