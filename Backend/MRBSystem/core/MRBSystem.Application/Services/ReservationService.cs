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
    private readonly IMapper _mapper;

    public ReservationService(
        IGenericRepository<Reservation> repository,
        IGenericRepository<RoomBlackout> blackoutRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repository = repository;
        _blackoutRepository = blackoutRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ResponseReservationDto> CreateAsync(CreateReservationDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var hasConflict = await _repository.GetQueryable()
            .AnyAsync(r => r.RoomId == dto.RoomId
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.AutoCancelled
                && dto.StartDate < r.EndDate
                && dto.EndDate > r.StartDate);

        if (hasConflict)
        {
            throw new InvalidOperationException("Room is already booked for the selected time range.");
        }

        var hasBlackout = await _blackoutRepository.GetQueryable()
            .AnyAsync(b => b.RoomId == dto.RoomId
                && dto.StartDate < b.EndDate
                && dto.EndDate > b.StartDate);

        if (hasBlackout)
        {
            throw new InvalidOperationException("Room is blacked out for the selected time range.");
        }

        var entity = _mapper.Map<Reservation>(dto);
        entity.ReservedByUserId = _currentUser.UserId;
        entity.QrCodeToken = Guid.NewGuid().ToString();

        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser)
            .Include(r => r.Organizer)
            .Include(r => r.CheckIn)
            .FirstAsync(r => r.Id == entity.Id);

        return _mapper.Map<ResponseReservationDto>(created);
    }

    public async Task<ResponseReservationDto> UpdateAsync(Guid id, UpdateReservationDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Reservation with id {id} not found.");

        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var hasConflict = await _repository.GetQueryable()
            .AnyAsync(r => r.Id != id
                && r.RoomId == dto.RoomId
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.AutoCancelled
                && dto.StartDate < r.EndDate
                && dto.EndDate > r.StartDate);

        if (hasConflict)
        {
            throw new InvalidOperationException("Room is already booked for the selected time range.");
        }

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser)
            .Include(r => r.Organizer)
            .Include(r => r.CheckIn)
            .FirstAsync(r => r.Id == id);

        return _mapper.Map<ResponseReservationDto>(updated);
    }

    public async Task CancelAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id)

        if (entity == null)
        {
            throw new NotFoundException($"Reservation with id {id} not found.");
        }

        entity.Status = ReservationStatus.Cancelled;
        await _repository.UpdateAsync(entity);
    }

    public async Task<ResponseReservationDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser)
            .Include(r => r.Organizer)
            .Include(r => r.CheckIn)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Reservation with id {id} not found.");

        return _mapper.Map<ResponseReservationDto>(entity);
    }

    public async Task<ICollection<ResponseReservationDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(r => r.Room)
            .Include(r => r.ReservedByUser)
            .Include(r => r.Organizer)
            .Include(r => r.CheckIn)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseReservationDto>>(entities);
    }
}
