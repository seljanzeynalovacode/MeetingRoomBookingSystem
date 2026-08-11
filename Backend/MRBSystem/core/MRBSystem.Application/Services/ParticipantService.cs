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
    private readonly IMapper _mapper;

    public ParticipantService(IGenericRepository<Participant> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ResponseParticipantDto> CreateAsync(CreateParticipantDto dto)
    {
        if (dto.UserId is null && string.IsNullOrWhiteSpace(dto.GuestEmail))
        {
            throw new ArgumentException("Either UserId or GuestEmail must be provided.");
        }

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

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(p => p.User)
            .FirstAsync(p => p.Id == id);

        return _mapper.Map<ResponseParticipantDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseParticipantDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Participant with id {id} not found.");

        return _mapper.Map<ResponseParticipantDto>(entity);
    }

    public async Task<ICollection<ResponseParticipantDto>> GetByReservationIdAsync(Guid reservationId)
    {
        var entities = await _repository.GetQueryable()
            .Where(p => p.ReservationId == reservationId)
            .Include(p => p.User)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseParticipantDto>>(entities);
    }
}
