using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Tests;

namespace MRBSystem.Testing;

public class RoomBlackoutServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly RoomBlackoutService _service;
    private readonly Guid _roomId = Guid.NewGuid();

    public RoomBlackoutServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.IsInRole("Admin")).Returns(true);

        _service = new RoomBlackoutService(
            new GenericRepository<RoomBlackout>(_context),
            new GenericRepository<Room>(_context),
            new GenericRepository<Reservation>(_context),
            new FakeUnitOfWork(),
            currentUser.Object,
            mapperConfig.CreateMapper(),
            new Mock<IRoomCalendarBroadcastService>().Object);

        SeedRoom();
    }

    [Fact]
    public async Task CreateAsync_ValidRoom_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateRoomBlackoutDto
        {
            RoomId = _roomId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Reason = "Temir"
        });

        result.Should().NotBeNull();
        result.Reason.Should().Be("Temir");
    }

    [Fact]
    public async Task CreateAsync_NonExistentRoom_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(new CreateRoomBlackoutDto
        {
            RoomId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Reason = "Temir"
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var act = () => _service.CreateAsync(new CreateRoomBlackoutDto
        {
            RoomId = _roomId,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(1),
            Reason = "Temir"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_StartDateAfterEndDate_ThrowsArgumentException()
    {
        var created = await _service.CreateAsync(new CreateRoomBlackoutDto
        {
            RoomId = _roomId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            Reason = "Temir"
        });

        var act = () => _service.UpdateAsync(created.Id, new UpdateRoomBlackoutDto
        {
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Yenilenmis sebeb"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private void SeedRoom()
    {
        var building = new Building { Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Name = "1-ci mertebe", LevelNumber = 1, Building = building };
        var room = new Room { Id = _roomId, Name = "Test Room", Capacity = 10, Floor = floor };

        _context.Rooms.Add(room);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
