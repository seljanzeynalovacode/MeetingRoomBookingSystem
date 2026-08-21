using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.AuthDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Testing;

namespace MRBSystem.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
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
        _emailServiceMock = new Mock<IEmailService>();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _authService = new AuthService(
     _userManagerMock.Object,
     null!,
     null!,
     new GenericRepository<RefreshToken>(_context),
     new GenericRepository<Reservation>(_context),
     currentUser.Object,
     configuration.Object,
     mapperConfig.CreateMapper(),
     NullLogger<AuthService>.Instance,
     _emailServiceMock.Object,
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

    [Fact]
    public async Task ForgotPasswordAsync_ExistingUser_SendsEmail()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target", Email = "target@example.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(targetUser)).ReturnsAsync("reset-token-123");

        await _authService.ForgotPasswordAsync(new ForgotPasswordDto { Email = "target@example.com" });

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            "target@example.com",
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains("reset-token-123"))), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistentUser_DoesNotThrowAndDoesNotSendEmail()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("ghost@example.com")).ReturnsAsync((AppUser?)null);

        var act = () => _authService.ForgotPasswordAsync(new ForgotPasswordDto { Email = "ghost@example.com" });

        await act.Should().NotThrowAsync();
        _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_Succeeds()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target2", Email = "target2@example.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target2@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(targetUser, "valid-token", "NewPass123!")).ReturnsAsync(IdentityResult.Success);

        var act = () => _authService.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = "target2@example.com",
            Token = "valid-token",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ThrowsInvalidOperation()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target3", Email = "target3@example.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target3@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(targetUser, "bad-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var act = () => _authService.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = "target3@example.com",
            Token = "bad-token",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_NonExistentUser_ThrowsInvalidOperation()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("ghost2@example.com")).ReturnsAsync((AppUser?)null);

        var act = () => _authService.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = "ghost2@example.com",
            Token = "whatever",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_Succeeds()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target4", Email = "target4@example.com", EmailConfirmed = false };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target4@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.ConfirmEmailAsync(targetUser, "valid-code")).ReturnsAsync(IdentityResult.Success);

        var act = () => _authService.ConfirmEmailAsync(new ConfirmEmailDto { Email = "target4@example.com", ConfirmationCode = "valid-code" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfirmEmailAsync_InvalidToken_ThrowsInvalidOperation()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target5", Email = "target5@example.com", EmailConfirmed = false };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target5@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.ConfirmEmailAsync(targetUser, "bad-code"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid code." }));

        var act = () => _authService.ConfirmEmailAsync(new ConfirmEmailDto { Email = "target5@example.com", ConfirmationCode = "bad-code" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_AlreadyConfirmed_DoesNotThrowAndDoesNotCallIdentity()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target6", Email = "target6@example.com", EmailConfirmed = true };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target6@example.com")).ReturnsAsync(targetUser);

        var act = () => _authService.ConfirmEmailAsync(new ConfirmEmailDto { Email = "target6@example.com", ConfirmationCode = "irrelevant" });

        await act.Should().NotThrowAsync();
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_UnconfirmedUser_SendsEmail()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target7", Email = "target7@example.com", EmailConfirmed = false };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target7@example.com")).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(targetUser)).ReturnsAsync("resend-code");

        await _authService.ResendConfirmationEmailAsync(new ResendConfirmationEmailDto { Email = "target7@example.com" });

        _emailServiceMock.Verify(x => x.SendEmailAsync(
            "target7@example.com",
            It.IsAny<string>(),
            It.Is<string>(b => b.Contains("resend-code"))), Times.Once);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_AlreadyConfirmedUser_DoesNotSendEmail()
    {
        var targetUser = new AppUser { Id = Guid.NewGuid(), FirstName = "Target", LastName = "User", UserName = "target8", Email = "target8@example.com", EmailConfirmed = true };
        _userManagerMock.Setup(x => x.FindByEmailAsync("target8@example.com")).ReturnsAsync(targetUser);

        await _authService.ResendConfirmationEmailAsync(new ResendConfirmationEmailDto { Email = "target8@example.com" });

        _emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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