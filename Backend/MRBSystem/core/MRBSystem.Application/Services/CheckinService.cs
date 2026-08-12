using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<CheckinService> _logger;

    public CheckinService(
        IGenericRepository<Checkin> repository,
        IGenericRepository<Reservation> reservationRepository,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<CheckinService> logger)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ResponseCheckinDto> CheckInAsync(CreateCheckinDto dto)
    {
        var reservation = await _reservationRepository.GetQueryable()
            .Include(r => r.CheckIn)
            .FirstOrDefaultAsync(r => r.QrCodeToken == dto.QrCodeToken);
        if (reservation is null) 
        {
            _logger.LogWarning("Attempt to check in with invalid QR code token: {QrCodeToken}.", dto.QrCodeToken);
            throw new NotFoundException("Invalid QR code token.");
        }

        if (reservation.CheckIn != null)
        {
            _logger.LogWarning("Attempt to check in for reservation {ReservationId} which is already checked in.", reservation.Id);
            throw new InvalidOperationException("This reservation is already checked in.");
        }

        if (DateTime.UtcNow > reservation.CheckinDeadline)
        {
            _logger.LogWarning("Attempt to check in for reservation {ReservationId} after the check-in deadline.", reservation.Id);
            throw new InvalidOperationException("Check-in deadline has passed.");
        }

        var entity = new Checkin
        {
            ReservationId = reservation.Id,
            UserId = _currentUser.UserId,
            CheckinTime = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);

        reservation.Status = ReservationStatus.CheckedIn;
        await _reservationRepository.UpdateAsync(reservation);

        _logger.LogInformation("User {UserId} checked in for reservation {ReservationId}.", _currentUser.UserId, reservation.Id);

        var created = await _repository.GetQueryable()
            .Include(c => c.User)
            .FirstAsync(c => c.Id == entity.Id);
        _logger.LogInformation("Checkin record created with id {CheckinId}.", created.Id);
        return _mapper.Map<ResponseCheckinDto>(created);
    }

    public async Task<ResponseCheckinDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity is null) 
        {
            _logger.LogWarning("Checkin with id {CheckinId} not found.", id);
            throw new NotFoundException($"Checkin with id {id} not found.");
        }
        _logger.LogInformation("Retrieved checkin with id {CheckinId}.", id);
        return _mapper.Map<ResponseCheckinDto>(entity);
    }
}
