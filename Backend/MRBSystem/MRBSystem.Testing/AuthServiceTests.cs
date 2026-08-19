using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Testing;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MRBSystem.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly AuthService _authService;
    private readonly Guid _adminId = Guid.NewGuid();

    public AuthServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        _userManagerMock = CreateUserManagerMock();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(_adminId);

        var configuration = new Mock<IConfiguration>();
        var emailService = new Mock<IEmailService>();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        // SignInManager ve RoleManager DeleteUserAsync-de istifade olunmur,
        // Castle Proxy-nin bunlari mock etmesi kovrek oldugu ucun birbasa null! otururuk
        _authService = new AuthService(
           _userManagerMock.Object,
           null!,
           null!,
           new GenericRepository<RefreshToken>(_context),
           new GenericRepository<Reservation>(_context),
           new GenericRepository<Comment>(_context),
           new GenericRepository<Like>(_context),
           currentUser.Object,
           configuration.Object,
           mapperConfig.CreateMapper(),
           NullLogger<AuthService>.Instance,
           emailService.Object,
           new UnitOfWork(_context));
    }

    [Fact]
    public async Task DeleteUserAsync_OnOwnAccount_ThrowsInvalidOperation()
    {
        var act = () => _authService.DeleteUserAsync(_adminId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Öz hesabınızı silə bilməzsiniz*");
    }

    [Fact]
    public async Task DeleteUserAsync_UserHasExistingReservation_ThrowsInvalidOperation()
    {
        var targetUserId = Guid.NewGuid();
        var targetUser = new AppUser { Id = targetUserId, FirstName = "Target", LastName = "User", UserName = "target", Email = "target@example.com" };
        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
        _context.Users.Add(targetUser);

        var building = new Building { Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Name = "1-ci mertebe", LevelNumber = 1, Building = building };
        var room = new Room { Name = "Test Room", Capacity = 10, Floor = floor };

        _context.Reservations.Add(new Reservation
        {
            Title = "Test Meeting",
            Room = room,
            ReservedByUserId = targetUserId,
            StartDate = DateTime.UtcNow.AddHours(1),
            EndDate = DateTime.UtcNow.AddHours(2)
        });
        await _context.SaveChangesAsync();

        var act = () => _authService.DeleteUserAsync(targetUserId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*rezervasiyaları*");
    }

    [Fact]
    public async Task DeleteUserAsync_UserWithNoRelatedData_Succeeds()
    {
        var targetUserId = Guid.NewGuid();
        var targetUser = new AppUser { Id = targetUserId, FirstName = "Target", LastName = "User", UserName = "target2", Email = "target2@example.com" };

        _userManagerMock.Setup(x => x.FindByIdAsync(targetUserId.ToString())).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.DeleteAsync(targetUser)).ReturnsAsync(IdentityResult.Success);

        var act = () => _authService.DeleteUserAsync(targetUserId);

        await act.Should().NotThrowAsync();
        _userManagerMock.Verify(x => x.DeleteAsync(targetUser), Times.Once);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}