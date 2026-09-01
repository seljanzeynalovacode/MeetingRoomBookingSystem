using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Tests;

namespace MRBSystem.Testing;

public class RoomServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly RoomService _roomService;
    private readonly Guid _buildingId = Guid.NewGuid();
    private readonly Guid _floorId = Guid.NewGuid();

    // 2024-01-08 = Bazar ertəsi (iş günü), saat 10:00 Bakı vaxtı = 06:00 UTC
    private static readonly DateTime WorkHourStart = new(2024, 1, 8, 6, 0, 0, DateTimeKind.Utc);

    public RoomServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _roomService = new RoomService(
            new GenericRepository<Room>(_context),
            new GenericRepository<Floor>(_context),
            new GenericRepository<Reservation>(_context),
            new GenericRepository<RoomBlackout>(_context),
            new GenericRepository<RoomEquipment>(_context),
            mapperConfig.CreateMapper(),
        new SystemSettingsService(
                new GenericRepository<SystemSettings>(_context),
                new FakeUnitOfWork(),
                mapperConfig.CreateMapper(),
                NullLogger<SystemSettingsService>.Instance),
        new WorkingHourService(
                new GenericRepository<BuildingWorkingHour>(_context),
                new GenericRepository<Building>(_context),
                new FakeUnitOfWork(),
                mapperConfig.CreateMapper(),
                NullLogger<WorkingHourService>.Instance));

        SeedBuildingAndFloor();
    }

    [Fact]
    public async Task SearchAvailableAsync_RoomWithNoConflicts_ReturnsRoom()
    {
        var room = await SeedRoomAsync(capacity: 10);

        var result = await _roomService.SearchAvailableAsync(new RoomAvailabilityRequestDto
        {
            BuildingId = _buildingId,
            Capacity = 5,
            StartDate = WorkHourStart,
            EndDate = WorkHourStart.AddHours(1)
        });

        result.Should().ContainSingle(r => r.RoomId == room.Id);
    }

    [Fact]
    public async Task SearchAvailableAsync_RoomBelowRequestedCapacity_IsExcluded()
    {
        await SeedRoomAsync(capacity: 3);

        var result = await _roomService.SearchAvailableAsync(new RoomAvailabilityRequestDto
        {
            BuildingId = _buildingId,
            Capacity = 10,
            StartDate = WorkHourStart,
            EndDate = WorkHourStart.AddHours(1)
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAvailableAsync_RoomWithOverlappingReservation_IsExcluded()
    {
        var room = await SeedRoomAsync(capacity: 10);
        var start = WorkHourStart;
        var end = WorkHourStart.AddHours(1);

        var user = new AppUser
        {
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser2",
            Email = "testuser2@example.com"
        };
        _context.Users.Add(user);

        _context.Reservations.Add(new Reservation
        {
            Title = "Existing meeting",
            RoomId = room.Id,
            ReservedByUserId = user.Id,
            StartDate = start,
            EndDate = end
        });
        await _context.SaveChangesAsync();

        var result = await _roomService.SearchAvailableAsync(new RoomAvailabilityRequestDto
        {
            BuildingId = _buildingId,
            Capacity = 5,
            StartDate = start.AddMinutes(30),
            EndDate = end.AddMinutes(30)
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAvailableAsync_RoomWithBlackout_IsExcluded()
    {
        var room = await SeedRoomAsync(capacity: 10);
        var start = WorkHourStart;
        var end = WorkHourStart.AddHours(1);

        _context.RoomBlackouts.Add(new RoomBlackout
        {
            RoomId = room.Id,
            StartDate = start,
            EndDate = end,
            Reason = "Temir"
        });
        await _context.SaveChangesAsync();

        var result = await _roomService.SearchAvailableAsync(new RoomAvailabilityRequestDto
        {
            BuildingId = _buildingId,
            Capacity = 5,
            StartDate = start,
            EndDate = end
        });

        result.Should().BeEmpty();
    }

    private void SeedBuildingAndFloor()
    {
        var building = new Building { Id = _buildingId, Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Id = _floorId, Name = "1-ci mertebe", LevelNumber = 1, BuildingId = _buildingId, Building = building };

        _context.Buildings.Add(building);
        _context.Floors.Add(floor);
        _context.SaveChanges();
    }

    private async Task<Room> SeedRoomAsync(int capacity)
    {
        var room = new Room
        {
            Name = "Test Room",
            Capacity = capacity,
            FloorId = _floorId,
            IsActive = true
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return room;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
