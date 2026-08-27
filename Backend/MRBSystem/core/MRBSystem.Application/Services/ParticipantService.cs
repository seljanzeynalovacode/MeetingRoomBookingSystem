using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.ParticipantDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class ParticipantService : IParticipantService
{
    private readonly IGenericRepository<Participant> _repository;
    private readonly IGenericRepository<Reservation> _reservationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public ParticipantService(
        IGenericRepository<Participant> repository,
        IGenericRepository<Reservation> reservationRepository,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<ResponseParticipantDto> CreateAsync(CreateParticipantDto dto)
    {
        if (dto.UserId is null && string.IsNullOrWhiteSpace(dto.GuestEmail))
        {
            throw new ArgumentException("Either UserId or GuestEmail must be provided.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(dto.ReservationId)
            ?? throw new NotFoundException($"Reservation with id {dto.ReservationId} not found.");

        EnsureCanManage(reservation);

        var entity = _mapper.Map<Participant>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(p => p.User)
            .FirstAsync(p => p.Id == entity.Id);

        return _mapper.Map<ResponseParticipantDto>(created);
    }

    public async Task<ResponseParticipantDto> UpdateAsync(Guid id, UpdateParticipantDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Participant with id {id} not found.");

        var reservation = await _reservationRepository.GetByIdAsync(entity.ReservationId)
            ?? throw new NotFoundException($"Reservation with id {entity.ReservationId} not found.");

        EnsureCanRespond(entity, reservation);

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(p => p.User)
            .FirstAsync(p => p.Id == id);

        return _mapper.Map<ResponseParticipantDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Participant with id {id} not found.");

        var reservation = await _reservationRepository.GetByIdAsync(entity.ReservationId)
            ?? throw new NotFoundException($"Reservation with id {entity.ReservationId} not found.");

        EnsureCanManage(reservation);

        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseParticipantDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(p => p.User)
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Participant with id {id} not found.");

        EnsureCanRespond(entity, entity.Reservation);

        return _mapper.Map<ResponseParticipantDto>(entity);
    }

    public async Task<ICollection<ResponseParticipantDto>> GetByReservationIdAsync(Guid reservationId)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId)
            ?? throw new NotFoundException($"Reservation with id {reservationId} not found.");

        var isOwnerOrAdmin = reservation.ReservedByUserId == _currentUser.UserId || _currentUser.IsInRole("Admin");
        var isParticipant = await _repository.GetQueryable()
            .AnyAsync(p => p.ReservationId == reservationId && p.UserId == _currentUser.UserId);

        if (!isOwnerOrAdmin && !isParticipant)
        {
            throw new ForbiddenException("Bu rezervasiyanın iştirakçılarına baxmaq hüququnuz yoxdur.");
        }

        var entities = await _repository.GetQueryable()
            .Where(p => p.ReservationId == reservationId)
            .Include(p => p.User)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseParticipantDto>>(entities);
    }

    private void EnsureCanManage(Reservation reservation)
    {
        var isOwner = reservation.ReservedByUserId == _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("Bu rezervasiyanın iştirakçılarını idarə etmək hüququnuz yoxdur.");
        }
    }

    private void EnsureCanRespond(Participant participant, Reservation reservation)
    {
        var isInvitedUser = participant.UserId.HasValue && participant.UserId == _currentUser.UserId;
        var isOwner = reservation.ReservedByUserId == _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isInvitedUser && !isOwner && !isAdmin)
        {
            throw new ForbiddenException("Bu iştirakçının statusunu dəyişmək hüququnuz yoxdur.");
        }
    }
}