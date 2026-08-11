using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.CheckinDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class CheckinService : ICheckinService
{
    private readonly IGenericRepository<Checkin> _repository;
    private readonly IGenericRepository<Reservation> _reservationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public CheckinService(
        IGenericRepository<Checkin> repository,
        IGenericRepository<Reservation> reservationRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ResponseCheckinDto> CheckInAsync(CreateCheckinDto dto)
    {
        var reservation = await _reservationRepository.GetQueryable()
            .Include(r => r.CheckIn)
            .FirstOrDefaultAsync(r => r.QrCodeToken == dto.QrCodeToken)
            ?? throw new NotFoundException("Invalid QR code token.");

        if (reservation.CheckIn is not null)
            throw new InvalidOperationException("This reservation is already checked in.");

        if (DateTime.UtcNow > reservation.CheckinDeadline)
            throw new InvalidOperationException("Check-in deadline has passed.");

        var entity = new Checkin
        {
            ReservationId = reservation.Id,
            UserId = _currentUser.UserId,
            CheckinTime = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);

        reservation.Status = ReservationStatus.CheckedIn;
        await _reservationRepository.UpdateAsync(reservation);

        var created = await _repository.GetQueryable()
            .Include(c => c.User)
            .FirstAsync(c => c.Id == entity.Id);

        return _mapper.Map<ResponseCheckinDto>(created);
    }

    public async Task<ResponseCheckinDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity is null) 
        {
            throw new NotFoundException($"Checkin with id {id} not found.");
        }

        return _mapper.Map<ResponseCheckinDto>(entity);
    }
}
