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
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IGenericRepository<RoomBlackout> _blackoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IGenericRepository<Reservation> repository,
        IGenericRepository<RoomBlackout> blackoutRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<ReservationService> logger,
        IGenericRepository<Room> roomRepository)
    {
        _repository = repository;
        _blackoutRepository = blackoutRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
        _roomRepository = roomRepository;
    }

    public async Task<ResponseReservationDto> CreateAsync(CreateReservationDto dto)
    {
        var room = await _roomRepository.GetByIdAsync(dto.RoomId);

        if (room == null)
        {
            throw new NotFoundException($"Room with id {dto.RoomId} not found.");
        }

        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await ValidateDatesAsync(dto.RoomId, dto.StartDate, dto.EndDate);

            var reservation = _mapper.Map<Reservation>(dto);
            reservation.ReservedByUserId = _currentUser.UserId;

            await _repository.AddAsync(reservation);
            return reservation;
        });

        _logger.LogInformation(
            "Reservation {ReservationId} created by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, dto.RoomId);

        var created = await GetWithDetailsAsync().FirstAsync(r => r.Id == entity.Id);

        await _notificationService.CreateAsync(created, NotificationType.Created);
        _logger.LogDebug(
            "Notification sent for created reservation {ReservationId} to relevant users",
            created.Id);

        return _mapper.Map<ResponseReservationDto>(created);
    }

    public async Task<ResponseReservationDto> UpdateAsync(Guid id, UpdateReservationDto dto)
    {
        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reservation = await _repository.GetByIdAsync(id);

            if (reservation == null)
            {
                _logger.LogWarning(
                    "Reservation with id {ReservationId} not found for update", id);

                throw new NotFoundException(
                    $"Reservation with id {id} not found.");
            }

            var room = await _roomRepository.GetByIdAsync(dto.RoomId);

            if (room == null)
            {
                throw new NotFoundException(
                    $"Room with id {dto.RoomId} not found.");
            }

            EnsureCanModify(reservation);

            await ValidateDatesAsync(
                dto.RoomId,
                dto.StartDate,
                dto.EndDate,
                excludeReservationId: id);

            _mapper.Map(dto, reservation);

            await _repository.UpdateAsync(reservation);

            return reservation;
        });

        _logger.LogInformation(
            "Reservation {ReservationId} updated by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, dto.RoomId);

        var updated = await GetWithDetailsAsync()
            .FirstAsync(r => r.Id == entity.Id);

        await _notificationService.CreateAsync(
            updated, NotificationType.Updated);

        _logger.LogDebug(
            "Notification sent for updated reservation {ReservationId} to relevant users",
            updated.Id);

        return _mapper.Map<ResponseReservationDto>(updated);
    }

    public async Task CancelAsync(Guid id)
    {
        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var reservation = await _repository.GetByIdAsync(id);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation with id {ReservationId} not found for cancellation", id);
                throw new NotFoundException($"Reservation with id {id} not found.");
            }

            EnsureCanModify(reservation);

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                _logger.LogWarning("Attempted to cancel an already cancelled reservation {ReservationId}", id);
                throw new InvalidOperationException("Bu rezervasiya artıq ləğv edilib!");
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _repository.UpdateAsync(reservation);
            return reservation;
        });

        _logger.LogInformation(
            "Reservation {ReservationId} cancelled by user {UserId} for room {RoomId}",
            entity.Id, _currentUser.UserId, entity.RoomId);

        var cancelled = await GetWithDetailsAsync().FirstAsync(r => r.Id == entity.Id);

        await _notificationService.CreateAsync(cancelled, NotificationType.Cancelled);
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
        var entities = await GetWithDetailsAsync()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return _mapper.Map<ICollection<ResponseReservationDto>>(entities);
    }

    private IQueryable<Reservation> GetWithDetailsAsync()
    {
        return _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser);
    }
    public async Task<ICollection<ResponseReservationDto>> GetByUserIdAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var entities = await GetWithDetailsAsync()
            .Where(r => r.ReservedByUserId == userId)
            .OrderBy(r => r.StartDate <= now && r.EndDate > now ? 0 : 1)
            .ThenByDescending(r => r.StartDate)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseReservationDto>>(entities);
    }
    public async Task<ICollection<ResponseReservationDto>> GetMyReservationsAsync()
    {
        var now = DateTime.UtcNow;
        var entities = await GetWithDetailsAsync()
            .Where(r => r.ReservedByUserId == _currentUser.UserId)
            .OrderBy(r => r.StartDate <= now && r.EndDate > now ? 0 : 1)
            .ThenByDescending(r => r.StartDate)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseReservationDto>>(entities);
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
        if (startDate < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Attempt to create/update reservation for room {RoomId} with a past StartDate {StartDate}",
                roomId, startDate);
            throw new ArgumentException("Keçmiş tarix və saata rezervasiya edilə bilməz.");
        }

        if (endDate - startDate < TimeSpan.FromMinutes(30))
        {
            _logger.LogWarning(
                "Reservation duration too short for room {RoomId}: {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new ArgumentException("Rezervasiya minimum 30 dəqiqə olmalıdır.");
        }

        if (endDate - startDate > TimeSpan.FromHours(4))
        {
            _logger.LogWarning(
                "Reservation duration too long for room {RoomId}: {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new ArgumentException("Rezervasiya maksimum 4 saat ola bilər.");
        }

        if (!_currentUser.IsInRole("Admin") && startDate > DateTime.UtcNow.AddDays(30))
        {
            _logger.LogWarning(
                "User {UserId} attempted to create/update reservation for room {RoomId} beyond the 30-day booking horizon: {StartDate}",
                _currentUser.UserId, roomId, startDate);
            throw new ArgumentException("Rezervasiya yalnız növbəti 30 gün ərzində yaradıla bilər.");
        }

        var hasConflict = await _repository.GetQueryable()
            .AnyAsync(r => r.Id != excludeReservationId
                && r.RoomId == roomId
                && r.Status != ReservationStatus.Cancelled
                && startDate < r.EndDate
                && endDate > r.StartDate);

        if (hasConflict)
        {
            _logger.LogWarning(
                "Attempt to create/update reservation for room {RoomId} with conflicting time range {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new InvalidOperationException("Room is already booked for the selected time range.");
        }
        var conflictingBlackout = await _blackoutRepository.GetQueryable()
            .Where(b => b.RoomId == roomId
                && startDate < b.EndDate
                && endDate > b.StartDate)
            .OrderBy(b => b.EndDate)
            .FirstOrDefaultAsync();

        if (conflictingBlackout != null)
        {
            _logger.LogWarning(
                "Attempt to create/update reservation for room {RoomId} during blackout time range {StartDate} - {EndDate}",
                roomId, startDate, endDate);
            throw new InvalidOperationException(
                $"Otaq bu tarix aralığında istifadəyə bağlıdır. Səbəb: {conflictingBlackout.Reason}. " +
                $"Otaq {conflictingBlackout.EndDate:dd.MM.yyyy HH:mm} tarixindən sonra yenidən istifadəyə veriləcək.");
        }

    }

    private void EnsureCanModify(Reservation entity)
    {
        var isOwner = entity.ReservedByUserId == _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
        {
            _logger.LogWarning(
                     "User {UserId} attempted to modify/cancel reservation {ReservationId} without ownership.",
             _currentUser.UserId, entity.Id);
            throw new ForbiddenException("Bu rezervasiyanı dəyişmək hüququnuz yoxdur.");
        }
    }
}