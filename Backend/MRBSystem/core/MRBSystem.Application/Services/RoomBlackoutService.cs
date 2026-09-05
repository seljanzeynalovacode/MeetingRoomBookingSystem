using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;
using MRBSystem.Shared.Exceptions;

namespace MRBSystem.Application.Services;

public class RoomBlackoutService : IRoomBlackoutService
{
    private readonly IGenericRepository<RoomBlackout> _repository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IGenericRepository<Reservation> _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly IRoomCalendarBroadcastService _roomCalendarBroadcastService;

    public RoomBlackoutService(
    IGenericRepository<RoomBlackout> repository,
    IGenericRepository<Room> roomRepository,
    IGenericRepository<Reservation> reservationRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IMapper mapper,
    IRoomCalendarBroadcastService roomCalendarBroadcastService)
    {
        _repository = repository;
        _roomRepository = roomRepository;
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _roomCalendarBroadcastService = roomCalendarBroadcastService;
    }

    private void EnsureIsAdmin()
    {
        if (!_currentUser.IsInRole("Admin"))
        {
            throw new ForbiddenException("Bu əməliyyatı yalnız admin edə bilər.");
        }
    }

    public async Task<ResponseRoomBlackoutDto> CreateAsync(CreateRoomBlackoutDto dto)
    {
        EnsureIsAdmin();

        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var room = await _roomRepository.GetByIdAsync(dto.RoomId)
            ?? throw new NotFoundException($"Room with id {dto.RoomId} not found.");

        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var hasConflict = await _reservationRepository.GetQueryable()
                .AnyAsync(r => r.RoomId == dto.RoomId
                    && r.Status != ReservationStatus.Cancelled
                    && dto.StartDate < r.EndDate
                    && dto.EndDate > r.StartDate);

            if (hasConflict)
            {
                throw new InvalidOperationException("Bu tarix aralığında otaqda mövcud rezervasiya(lar) var, blackout yaradıla bilməz.");
            }

            var blackout = _mapper.Map<RoomBlackout>(dto);
            await _repository.AddAsync(blackout);
            return blackout;
        });

        var created = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .FirstAsync(rb => rb.Id == entity.Id);

        await _roomCalendarBroadcastService.NotifyRoomCalendarChangedAsync(created.RoomId);

        return _mapper.Map<ResponseRoomBlackoutDto>(created);
    }

    public async Task<ResponseRoomBlackoutDto> UpdateAsync(Guid id, UpdateRoomBlackoutDto dto)
    {
        EnsureIsAdmin();

        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var entity = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var blackout = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException($"RoomBlackout with id {id} not found.");

            var hasConflict = await _reservationRepository.GetQueryable()
                .AnyAsync(r => r.RoomId == blackout.RoomId
                    && r.Status != ReservationStatus.Cancelled
                    && dto.StartDate < r.EndDate
                    && dto.EndDate > r.StartDate);

            if (hasConflict)
            {
                throw new InvalidOperationException("Bu tarix aralığında otaqda mövcud rezervasiya(lar) var, blackout yenilənə bilməz.");
            }

            _mapper.Map(dto, blackout);
            await _repository.UpdateAsync(blackout);
            return blackout;
        });

        var updated = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .FirstAsync(rb => rb.Id == id);

        await _roomCalendarBroadcastService.NotifyRoomCalendarChangedAsync(updated.RoomId);

        return _mapper.Map<ResponseRoomBlackoutDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        EnsureIsAdmin();

        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"RoomBlackout with id {id} not found.");

        await _repository.DeleteAsync(id);

        await _roomCalendarBroadcastService.NotifyRoomCalendarChangedAsync(entity.RoomId);
    }

    public async Task<ResponseRoomBlackoutDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .FirstOrDefaultAsync(rb => rb.Id == id)
            ?? throw new NotFoundException($"RoomBlackout with id {id} not found.");

        return _mapper.Map<ResponseRoomBlackoutDto>(entity);
    }

    public async Task<ICollection<ResponseRoomBlackoutDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .OrderByDescending(rb => rb.CreatedAt)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseRoomBlackoutDto>>(entities);
    }
}