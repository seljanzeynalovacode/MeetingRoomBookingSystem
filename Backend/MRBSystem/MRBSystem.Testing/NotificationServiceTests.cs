using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Application.Services;

namespace MRBSystem.Testing;

public class NotificationServiceTests :IDisposable
{
    private readonly TestMRBAppDbContext _context;

    public NotificationServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();
    }

    private NotificationService CreateService(Guid currentUserId)
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId);

        return new NotificationService(
            new GenericRepository<Notification>(_context),
            currentUser.Object,
            mapperConfig.CreateMapper());
    }

    private async Task<Guid> SeedUserAsync(string username)
    {
        var id = Guid.NewGuid();
        _context.Users.Add(new AppUser { Id = id, FirstName = "F", LastName = "L", UserName = username, Email = $"{username}@example.com" });
        await _context.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsOnlyCurrentUsersNotifications()
    {
        var userId = await SeedUserAsync("notifowner");
        var otherUserId = await SeedUserAsync("notifother");

        _context.Notifications.Add(new Notification { UserId = userId, Title = "Mine", Message = "Msg", Type = NotificationType.Created, ScheduledFor = DateTime.UtcNow });
        _context.Notifications.Add(new Notification { UserId = otherUserId, Title = "NotMine", Message = "Msg", Type = NotificationType.Created, ScheduledFor = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var service = CreateService(userId);
        var result = await service.GetMyNotificationsAsync();

        result.Should().ContainSingle(n => n.Title == "Mine");
    }

    [Fact]
    public async Task MarkAsReadAsync_ByOwner_Succeeds()
    {
        var userId = await SeedUserAsync("notifowner2");
        var notification = new Notification { UserId = userId, Title = "T", Message = "M", Type = NotificationType.Created, ScheduledFor = DateTime.UtcNow };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var service = CreateService(userId);
        await service.MarkAsReadAsync(notification.Id);

        var updated = _context.Notifications.First(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_ByNonOwner_ThrowsUnauthorized()
    {
        var ownerId = await SeedUserAsync("notifowner3");
        var notification = new Notification { UserId = ownerId, Title = "T", Message = "M", Type = NotificationType.Created, ScheduledFor = DateTime.UtcNow };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var service = CreateService(Guid.NewGuid());

        var act = () => service.MarkAsReadAsync(notification.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistentNotification_ThrowsNotFound()
    {
        var service = CreateService(Guid.NewGuid());

        var act = () => service.MarkAsReadAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_PersistsNotificationLinkedToReservation()
    {
        var userId = await SeedUserAsync("notifreserver");
        var user = _context.Users.First(u => u.Id == userId);

        var building = new Building { Name = "B", Address = "A" };
        var floor = new Floor { Name = "F", LevelNumber = 1, Building = building };
        var room = new Room { Name = "Room", Capacity = 5, Floor = floor };

        var reservation = new Reservation
        {
            Title = "Test Meeting",
            ReservedByUserId = userId,
            ReservedByUser = user,
            Room = room,
            StartDate = DateTime.UtcNow.AddHours(1),
            EndDate = DateTime.UtcNow.AddHours(2)
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        var service = CreateService(userId);
        await service.CreateAsync(reservation, NotificationType.Created);

        var notifications = _context.Notifications.ToList();
        notifications.Should().ContainSingle(n => n.UserId == userId && n.ReservationId == reservation.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
