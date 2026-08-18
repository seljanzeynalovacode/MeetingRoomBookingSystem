using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.ReservationDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Testing;
using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace MRBSystem.Tests;

public class ReservationServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly ReservationService _reservationService;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _roomId = Guid.NewGuid();

    public ReservationServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(_userId);
        currentUser.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);

        var notificationService = new Mock<INotificationService>();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _reservationService = new ReservationService(
            new GenericRepository<Reservation>(_context),
            new GenericRepository<RoomBlackout>(_context),
            new FakeUnitOfWork(),
            currentUser.Object,
            notificationService.Object,
            mapperConfig.CreateMapper(),
            NullLogger<ReservationService>.Instance,
            new GenericRepository<Room>(_context));
    }

    [Fact]
    public async Task CreateAsync_NoConflict_CreatesReservation()
    {
        await SeedRoomAndUserAsync();

        var dto = new CreateReservationDto
        {
            Title = "Sprint Planning",
            RoomId = _roomId,
            StartDate = DateTime.UtcNow.AddHours(1),
            EndDate = DateTime.UtcNow.AddHours(2)
        };

        var result = await _reservationService.CreateAsync(dto);

        result.Title.Should().Be("Sprint Planning");
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task CreateAsync_OverlappingReservation_ThrowsConflict()
    {
        await SeedRoomAndUserAsync();

        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(2);

        await _reservationService.CreateAsync(new CreateReservationDto
        {
            Title = "Ilk bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = end
        });

        var secondBooking = new CreateReservationDto
        {
            Title = "Ust-uste dusen bron",
            RoomId = _roomId,
            StartDate = start.AddMinutes(30),
            EndDate = end.AddMinutes(30)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(secondBooking))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already booked*");
    }

    private async Task SeedRoomAndUserAsync()
    {
        var user = new AppUser
        {
            Id = _userId,
            FirstName = "Test",
            LastName = "User",
            UserName = "testuser",
            Email = "test@example.com"
        };

        var building = new Building { Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Name = "1-ci mertebe", LevelNumber = 1, Building = building };
        var room = new Room { Id = _roomId, Name = "Test Room", Capacity = 10, Floor = floor };

        _context.Users.Add(user);
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

// SQLite Serializable-i tam deteklemir, ona gore testde real tranzaksiya acmadan
// birbasa operation-u icra edirik
public class FakeUnitOfWork : IUnitOfWork
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        return await operation();
    }
}