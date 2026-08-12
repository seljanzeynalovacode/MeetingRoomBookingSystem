using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

    public ReservationService(
        IGenericRepository<Reservation> repository,
        IGenericRepository<RoomBlackout> blackoutRepository,
        ICurrentUserService currentUser,
        INotificationService notificationService,
        IMapper mapper)
    {
        _repository = repository;
        _blackoutRepository = blackoutRepository;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<ResponseReservationDto> CreateAsync(CreateReservationDto dto)
    {
        await ValidateDatesAsync(dto.RoomId, dto.StartDate, dto.EndDate);

        var entity = _mapper.Map<Reservation>(dto);
        entity.ReservedByUserId = _currentUser.UserId;
        entity.QrCodeToken = Guid.NewGuid().ToString();
        entity.CheckinDeadline = dto.StartDate.AddMinutes(15); // ne zaman autocancel olsun

        await _repository.AddAsync(entity);

        var created = await GetWithDetailsAsync().FirstAsync(r => r.Id == entity.Id);

        await _notificationService.CreateAndSendAsync(created, NotificationType.Created);

        return _mapper.Map<ResponseReservationDto>(created);
    }

    public async Task<ResponseReservationDto> UpdateAsync(Guid id, UpdateReservationDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException($"Reservation with id {id} not found.");
        }

        await ValidateDatesAsync(dto.RoomId, dto.StartDate, dto.EndDate, excludeReservationId: id);

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await GetWithDetailsAsync().FirstAsync(r => r.Id == id);

        await _notificationService.CreateAndSendAsync(updated, NotificationType.Updated);

        return _mapper.Map<ResponseReservationDto>(updated);
    }

    public async Task CancelAsync(Guid id)
    {
        var entity = await GetWithDetailsAsync().FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null){
            throw new NotFoundException($"Reservation with id {id} not found.");
        }
        entity.Status = ReservationStatus.Cancelled;
        await _repository.UpdateAsync(entity);

        await _notificationService.CreateAndSendAsync(entity, NotificationType.Cancelled);
    }

    public async Task<ResponseReservationDto> GetByIdAsync(Guid id)
    {
        var entity = await GetWithDetailsAsync().FirstOrDefaultAsync(r => r.Id == id);
        if (entity == null) {
            throw new NotFoundException($"Reservation with id {id} not found.");
        }
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
            throw new ArgumentException("StartDate must be earlier than EndDate.");

        var hasConflict = await _repository.GetQueryable()
            .AnyAsync(r => r.Id != excludeReservationId
                && r.RoomId == roomId
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.AutoCancelled
                && startDate < r.EndDate
                && endDate > r.StartDate);

        if (hasConflict)
        {
            throw new InvalidOperationException("Room is already booked for the selected time range.");
        }
        var hasBlackout = await _blackoutRepository.GetQueryable()
            .AnyAsync(b => b.RoomId == roomId
                && startDate < b.EndDate
                && endDate > b.StartDate);

        if (hasBlackout)
        {
            throw new InvalidOperationException("Room is blacked out for the selected time range.");
        }
    }
}
