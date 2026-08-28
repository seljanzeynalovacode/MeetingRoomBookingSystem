using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.ReservationDtos;
using MRBSystem.Contracts.DTOs.SystemSettingsDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Testing;
using System.Data;

namespace MRBSystem.Tests;

public class ReservationServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly ReservationService _reservationService;
    private readonly SystemSettingsService _systemSettingsService;
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

        _systemSettingsService = new SystemSettingsService(
            new GenericRepository<SystemSettings>(_context),
            new FakeUnitOfWork(),
            mapperConfig.CreateMapper(),
            NullLogger<SystemSettingsService>.Instance);

        _reservationService = new ReservationService(
            new GenericRepository<Reservation>(_context),
            new GenericRepository<RoomBlackout>(_context),
            new FakeUnitOfWork(),
            currentUser.Object,
            notificationService.Object,
            mapperConfig.CreateMapper(),
            NullLogger<ReservationService>.Instance,
            new GenericRepository<Room>(_context),
            new WorkingHourService(
                new GenericRepository<BuildingWorkingHour>(_context),
                new GenericRepository<Building>(_context),
                new FakeUnitOfWork(),
                mapperConfig.CreateMapper(),
                NullLogger<WorkingHourService>.Instance),
            _systemSettingsService);
    }

    [Fact]
    public async Task CreateAsync_NoConflict_CreatesReservation()
    {
        await SeedRoomAndUserAsync();

        var start = NextLocalTime(10);
        var dto = new CreateReservationDto
        {
            Title = "Sprint Planning",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddHours(1)
        };

        var result = await _reservationService.CreateAsync(dto);

        result.Title.Should().Be("Sprint Planning");
        result.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task CreateAsync_OverlappingReservation_ThrowsConflict()
    {
        await SeedRoomAndUserAsync();

        var start = NextLocalTime(10);
        var end = start.AddHours(1);

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

    [Fact]
    public async Task CreateAsync_PastStartDate_ThrowsArgumentException()
    {
        await SeedRoomAndUserAsync();

        var dto = new CreateReservationDto
        {
            Title = "Kecmis bron",
            RoomId = _roomId,
            StartDate = DateTime.UtcNow.AddHours(-2),
            EndDate = DateTime.UtcNow.AddHours(-1)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(dto))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_DurationLessThan30Minutes_ThrowsArgumentException()
    {
        await SeedRoomAndUserAsync();

        var start = NextLocalTime(10);
        var dto = new CreateReservationDto
        {
            Title = "Cox qisa bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddMinutes(15)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(dto))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_DurationMoreThan4Hours_ThrowsArgumentException()
    {
        await SeedRoomAndUserAsync();

        var start = NextLocalTime(10);
        var dto = new CreateReservationDto
        {
            Title = "Cox uzun bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddHours(5)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(dto))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_OutsideWorkingHours_ThrowsArgumentException()
    {
        await SeedRoomAndUserAsync();

        var start = NextLocalTime(10);
        var localDay = start.AddHours(4).DayOfWeek;

        var room = await _context.Rooms.Include(r => r.Floor).FirstAsync(r => r.Id == _roomId);
        var closedRow = await _context.BuildingWorkingHours
            .FirstAsync(w => w.BuildingId == room.Floor.BuildingId && w.DayOfWeek == localDay);
        closedRow.IsWorkingDay = false;
        await _context.SaveChangesAsync();

        var dto = new CreateReservationDto
        {
            Title = "Is saatindan kenar",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddHours(1)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(dto))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WithinBufferWindow_ThrowsConflict()
    {
        await SeedRoomAndUserAsync();
        await _systemSettingsService.UpdateAsync(BuildSettingsDto(bufferMinutes: 15));

        var start = NextLocalTime(10);
        var end = start.AddHours(1);

        await _reservationService.CreateAsync(new CreateReservationDto
        {
            Title = "Ilk bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = end
        });

        var secondBooking = new CreateReservationDto
        {
            Title = "Bufer daxilinde bron",
            RoomId = _roomId,
            StartDate = end.AddMinutes(5),
            EndDate = end.AddMinutes(35)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(secondBooking))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hazırlıq fasiləsi*");
    }

    [Fact]
    public async Task CreateAsync_ActiveReservationLimitReached_ThrowsInvalidOperationException()
    {
        await SeedRoomAndUserAsync();
        await _systemSettingsService.UpdateAsync(BuildSettingsDto(
            isActiveReservationLimitEnabled: true,
            maxActiveReservationsPerUser: 1));

        var start = NextLocalTime(10);
        await _reservationService.CreateAsync(new CreateReservationDto
        {
            Title = "Birinci aktiv bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddHours(1)
        });

        var secondStart = start.AddHours(3);
        var secondBooking = new CreateReservationDto
        {
            Title = "Limitden sonraki bron",
            RoomId = _roomId,
            StartDate = secondStart,
            EndDate = secondStart.AddHours(1)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(secondBooking))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*aktiv rezervasiyanız*");
    }

    [Fact]
    public async Task CreateAsync_BeyondBookingHorizon_ThrowsArgumentException()
    {
        await SeedRoomAndUserAsync();
        await _systemSettingsService.UpdateAsync(BuildSettingsDto(bookingHorizonDays: 1));

        var start = DateTime.UtcNow.AddDays(5);
        var dto = new CreateReservationDto
        {
            Title = "Horizon xarici bron",
            RoomId = _roomId,
            StartDate = start,
            EndDate = start.AddHours(1)
        };

        await FluentActions.Invoking(() => _reservationService.CreateAsync(dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*gün ərzində*");
    }

    private static UpdateSystemSettingsDto BuildSettingsDto(
        int bufferMinutes = 0,
        int bookingHorizonDays = 30,
        bool isActiveReservationLimitEnabled = false,
        int maxActiveReservationsPerUser = 5)
    {
        return new UpdateSystemSettingsDto
        {
            MinReservationMinutes = 30,
            MaxReservationHours = 4,
            BufferMinutes = bufferMinutes,
            BookingHorizonDays = bookingHorizonDays,
            IsActiveReservationLimitEnabled = isActiveReservationLimitEnabled,
            MaxActiveReservationsPerUser = maxActiveReservationsPerUser,
            MaxCalendarRangeDays = 90
        };
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

        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            _context.BuildingWorkingHours.Add(new BuildingWorkingHour
            {
                BuildingId = building.Id,
                DayOfWeek = day,
                IsWorkingDay = true,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 59)
            });
        }

        await _context.SaveChangesAsync();
    }

    private static DateTime NextLocalTime(int hour)
    {
        var nowLocal = DateTime.UtcNow.AddHours(4);
        var candidate = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, hour, 0, 0);
        if (candidate <= nowLocal) candidate = candidate.AddDays(1);
        return candidate.AddHours(-4);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
