using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class RoomBlackoutService : IRoomBlackoutService
{
    private readonly IGenericRepository<RoomBlackout> _repository;
    private readonly IGenericRepository<Room> _roomRepository;
    private readonly IMapper _mapper;

    public RoomBlackoutService(
        IGenericRepository<RoomBlackout> repository,
        IGenericRepository<Room> roomRepository,
        IMapper mapper)
    {
        _repository = repository;
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<ResponseRoomBlackoutDto> CreateAsync(CreateRoomBlackoutDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var room = await _roomRepository.GetByIdAsync(dto.RoomId)
            ?? throw new NotFoundException($"Room with id {dto.RoomId} not found.");

        var entity = _mapper.Map<RoomBlackout>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .FirstAsync(rb => rb.Id == entity.Id);

        return _mapper.Map<ResponseRoomBlackoutDto>(created);
    }

    public async Task<ResponseRoomBlackoutDto> UpdateAsync(Guid id, UpdateRoomBlackoutDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"RoomBlackout with id {id} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(rb => rb.Room)
            .FirstAsync(rb => rb.Id == id);

        return _mapper.Map<ResponseRoomBlackoutDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
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
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseRoomBlackoutDto>>(entities);
    }
}