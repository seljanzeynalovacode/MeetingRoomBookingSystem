using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Contracts.DTOs.CalendarDtos;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Domain.Repository;

namespace MRBSystem.Application.Services;

public class RoomService : IRoomService
{
    private readonly IGenericRepository<Room> _repository;
    private readonly IGenericRepository<Floor> _floorRepository;
    private readonly IGenericRepository<Reservation> _reservationRepository;
    private readonly IGenericRepository<RoomBlackout> _blackoutRepository;
    private readonly IGenericRepository<RoomEquipment> _roomEquipmentRepository;
    private readonly IMapper _mapper;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IWorkingHourService _workingHourService;
    private readonly ITimeZoneService _timeZoneService;

    public RoomService(
        IGenericRepository<Room> repository,
        IGenericRepository<Floor> floorRepository,
        IGenericRepository<Reservation> reservationRepository,
        IGenericRepository<RoomBlackout> blackoutRepository,
        IGenericRepository<RoomEquipment> roomEquipmentRepository,
        IMapper mapper,
        ISystemSettingsService systemSettingsService,
        IWorkingHourService workingHourService,
        ITimeZoneService timeZoneService)
    {
        _repository = repository;
        _floorRepository = floorRepository;
        _reservationRepository = reservationRepository;
        _blackoutRepository = blackoutRepository;
        _roomEquipmentRepository = roomEquipmentRepository;
        _mapper = mapper;
        _systemSettingsService = systemSettingsService;
        _workingHourService = workingHourService;
        _timeZoneService = timeZoneService;
    }
    public async Task<ResponseRoomDto> CreateAsync(CreateRoomDto dto)
    {
        var floor = await _floorRepository.GetByIdAsync(dto.FloorId)
            ?? throw new NotFoundException($"Floor with id {dto.FloorId} not found.");

        var entity = _mapper.Map<Room>(dto);
        await _repository.AddAsync(entity);

        var created = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstAsync(r => r.Id == entity.Id);

        return _mapper.Map<ResponseRoomDto>(created);
    }

    public async Task<ResponseRoomDto> UpdateAsync(Guid id, UpdateRoomDto dto)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Room with id {id} not found.");

        var floor = await _floorRepository.GetByIdAsync(dto.FloorId)
            ?? throw new NotFoundException($"Floor with id {dto.FloorId} not found.");

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);

        var updated = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstAsync(r => r.Id == id);

        return _mapper.Map<ResponseRoomDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<ResponseRoomDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Room with id {id} not found.");

        return _mapper.Map<ResponseRoomDto>(entity);
    }

    public async Task<ICollection<ResponseRoomDto>> GetAllAsync()
    {
        var entities = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .OrderByDescending(r => r.Floor.Building.CreatedAt)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return _mapper.Map<ICollection<ResponseRoomDto>>(entities);
    }

    public async Task<ICollection<ResponseAvailableRoomDto>> SearchAvailableAsync(RoomAvailabilityRequestDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
        {
            throw new ArgumentException("StartDate must be earlier than EndDate.");
        }

        var localStart = _timeZoneService.ToLocal(dto.StartDate);
        var localEnd = _timeZoneService.ToLocal(dto.EndDate);

        await _workingHourService.EnsureWithinWorkingHoursAsync(dto.BuildingId, localStart, localEnd);

        var candidateRooms = await _repository.GetQueryable()
            .Include(r => r.Floor)
                .ThenInclude(f => f.Building)
            .Where(r => r.IsActive
                && r.Floor.BuildingId == dto.BuildingId
                && (dto.FloorId == null || r.FloorId == dto.FloorId)
                && r.Capacity >= dto.Capacity)
            .ToListAsync();

        if (candidateRooms.Count == 0)
        {
            return Array.Empty<ResponseAvailableRoomDto>();
        }

        var candidateRoomIds = candidateRooms.Select(r => r.Id).ToList();

        if (dto.EquipmentIds is { Count: > 0 })
        {
            var roomEquipmentPairs = await _roomEquipmentRepository.GetQueryable()
                .Where(re => candidateRoomIds.Contains(re.RoomId))
                .Select(re => new { re.RoomId, re.EquipmentId })
                .ToListAsync();

            var roomsWithAllEquipment = roomEquipmentPairs
                .GroupBy(re => re.RoomId)
                .Where(g => dto.EquipmentIds.All(reqId => g.Any(re => re.EquipmentId == reqId)))
                .Select(g => g.Key)
                .ToHashSet();

            candidateRoomIds = candidateRoomIds.Where(id => roomsWithAllEquipment.Contains(id)).ToList();
            candidateRooms = candidateRooms.Where(r => candidateRoomIds.Contains(r.Id)).ToList();
        }

        if (candidateRoomIds.Count == 0)
        {
            return Array.Empty<ResponseAvailableRoomDto>();
        }

        var busyByReservation = await _reservationRepository.GetQueryable()
            .Where(r => candidateRoomIds.Contains(r.RoomId)
                && r.Status != ReservationStatus.Cancelled
                && dto.StartDate < r.EndDate
                && dto.EndDate > r.StartDate)
            .Select(r => r.RoomId)
            .Distinct()
            .ToListAsync();

        var busyByBlackout = await _blackoutRepository.GetQueryable()
            .Where(b => candidateRoomIds.Contains(b.RoomId)
                && dto.StartDate < b.EndDate
                && dto.EndDate > b.StartDate)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        var busyRoomIds = busyByReservation.Union(busyByBlackout).ToHashSet();

        var availableRooms = candidateRooms
            .Where(r => !busyRoomIds.Contains(r.Id))
            .ToList();

        if (availableRooms.Count == 0)
        {
            return Array.Empty<ResponseAvailableRoomDto>();
        }

        var availableRoomIds = availableRooms.Select(r => r.Id).ToList();

        var equipmentByRoom = await _roomEquipmentRepository.GetQueryable()
            .Where(re => availableRoomIds.Contains(re.RoomId))
            .Include(re => re.Equipment)
            .ToListAsync();

        return availableRooms.Select(r => new ResponseAvailableRoomDto
        {
            RoomId = r.Id,
            RoomName = r.Name,
            Capacity = r.Capacity,
            FloorName = r.Floor.Name,
            BuildingName = r.Floor.Building.Name,
            Equipment = equipmentByRoom
                .Where(re => re.RoomId == r.Id)
                .Select(re => re.Equipment.Name)
                .ToList()
        }).ToList();
    }

    public async Task<ICollection<RoomCalendarDto>> GetRoomCalendarAsync(Guid roomId, DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("StartDate must be earlier than or equal to EndDate.");
        }

        var settings = await _systemSettingsService.GetAsync();
        if ((endDate.Date - startDate.Date).TotalDays > settings.MaxCalendarRangeDays)
        {
            throw new ArgumentException($"Date range cannot exceed {settings.MaxCalendarRangeDays} days.");
        }

        var room = await _repository.GetByIdAsync(roomId)
            ?? throw new NotFoundException($"Room with id {roomId} not found.");

        var rangeStart = startDate.Date;
        var rangeEndExclusive = endDate.Date.AddDays(1);

        var reservations = await _reservationRepository.GetQueryable()
            .Include(r => r.ReservedByUser)
            .Where(r => r.RoomId == roomId
                && r.Status != ReservationStatus.Cancelled
                && r.StartDate < rangeEndExclusive
                && r.EndDate > rangeStart)
            .OrderBy(r => r.StartDate)
            .ToListAsync();

        var blackouts = await _blackoutRepository.GetQueryable()
            .Where(b => b.RoomId == roomId
                && b.StartDate < rangeEndExclusive
                && b.EndDate > rangeStart)
            .OrderBy(b => b.StartDate)
            .ToListAsync();

        var days = new List<RoomCalendarDto>();

        for (var day = rangeStart; day < rangeEndExclusive; day = day.AddDays(1))
        {
            var dayStart = day;
            var dayEnd = day.AddDays(1);

            var dayReservations = reservations
                .Where(r => r.StartDate < dayEnd && r.EndDate > dayStart)
                .Select(r => new CalendarReservationDto
                {
                    ReservationId = r.Id,
                    Title = r.Title,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    ReservedByUserName = r.ReservedByUser.FirstName + " " + r.ReservedByUser.LastName,
                    Status = r.Status.ToString()
                })
                .ToList();

            var dayBlackouts = blackouts
                .Where(b => b.StartDate < dayEnd && b.EndDate > dayStart)
                .Select(b => new CalendarBlackoutDto
                {
                    BlackoutId = b.Id,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Reason = b.Reason
                })
                .ToList();

            days.Add(new RoomCalendarDto
            {
                RoomId = room.Id,
                RoomName = room.Name,
                Date = day,
                Reservations = dayReservations,
                Blackouts = dayBlackouts
            });
        }

        return days;
    }
}