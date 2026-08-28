using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Shared.Exceptions;

namespace MRBSystem.Testing;

public class RoleServiceTests
{
    private readonly TestMRBAppDbContext _context;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        _roleManager = new RoleManager<AppRole>(
            new RoleStore<AppRole, MRBAppDbContext, Guid>(_context),
            new List<IRoleValidator<AppRole>> { new RoleValidator<AppRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<AppRole>>.Instance);

        _userManager = new UserManager<AppUser>(
            new UserStore<AppUser, AppRole, MRBAppDbContext, Guid>(_context),
            null,
            new PasswordHasher<AppUser>(),
            new List<IUserValidator<AppUser>>(),
            new List<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            NullLogger<UserManager<AppUser>>.Instance);

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _service = new RoleService(_roleManager, _userManager, currentUser.Object, mapperConfig.CreateMapper(), NullLogger<RoleService>.Instance);
    }

    [Fact]
    public async Task CreateRoleAsync_NewRole_Succeeds()
    {
        var result = await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Manager" });

        result.Name.Should().Be("Manager");
    }

    [Fact]
    public async Task CreateRoleAsync_ExistingRole_ThrowsInvalidOperation()
    {
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Manager" });

        var act = () => _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Manager" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateRoleAsync_ValidRole_Succeeds()
    {
        var created = await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Manager" });

        var result = await _service.UpdateRoleAsync(created.Id, new UpdateAppRoleDto { Name = "SeniorManager" });

        result.Name.Should().Be("SeniorManager");
    }

    [Fact]
    public async Task UpdateRoleAsync_NonExistentId_ThrowsNotFound()
    {
        var act = () => _service.UpdateRoleAsync(Guid.NewGuid(), new UpdateAppRoleDto { Name = "X" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteRoleAsync_ValidRole_RemovesIt()
    {
        var created = await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "TempRole" });

        await _service.DeleteRoleAsync(created.Id);

        var act = () => _service.GetRoleByIdAsync(created.Id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteRoleAsync_NonExistentId_ThrowsNotFound()
    {
        var act = () => _service.DeleteRoleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetRoleByIdAsync_NonExistentId_ThrowsNotFound()
    {
        var act = () => _service.GetRoleByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllCreatedRoles()
    {
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "RoleA" });
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "RoleB" });

        var result = await _service.GetAllRolesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_ValidUserAndRole_Succeeds()
    {
        var user = await SeedUserAsync("assignee");
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Reviewer" });

        await _service.AssignRoleToUserAsync(user.Id, "Reviewer");

        var roles = await _userManager.GetRolesAsync(user);
        roles.Should().Contain("Reviewer");
    }

    [Fact]
    public async Task AssignRoleToUserAsync_NonExistentUser_ThrowsNotFound()
    {
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Reviewer" });

        var act = () => _service.AssignRoleToUserAsync(Guid.NewGuid(), "Reviewer");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AssignRoleToUserAsync_NonExistentRole_ThrowsNotFound()
    {
        var user = await SeedUserAsync("norole");

        var act = () => _service.AssignRoleToUserAsync(user.Id, "GhostRole");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_ValidUserAndRole_Succeeds()
    {
        var user = await SeedUserAsync("removee");
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Reviewer" });
        await _service.AssignRoleToUserAsync(user.Id, "Reviewer");

        await _service.RemoveRoleFromUserAsync(user.Id, "Reviewer");

        var roles = await _userManager.GetRolesAsync(user);
        roles.Should().NotContain("Reviewer");
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_NonExistentUser_ThrowsNotFound()
    {
        var act = () => _service.RemoveRoleFromUserAsync(Guid.NewGuid(), "Reviewer");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsAssignedRoles()
    {
        var user = await SeedUserAsync("hasroles");
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Reviewer" });
        await _service.AssignRoleToUserAsync(user.Id, "Reviewer");

        var result = await _service.GetUserRolesAsync(user.Id);

        result.Should().Contain("Reviewer");
    }

    [Fact]
    public async Task GetUserRolesAsync_NonExistentUser_ThrowsNotFound()
    {
        var act = () => _service.GetUserRolesAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllUsersWithRolesAsync_ReturnsUsersWithTheirRoles()
    {
        var user = await SeedUserAsync("listeduser");
        await _service.CreateRoleAsync(new CreateAppRoleDto { Name = "Reviewer" });
        await _service.AssignRoleToUserAsync(user.Id, "Reviewer");

        var result = await _service.GetAllUsersWithRolesAsync();

        result.Should().ContainSingle(u => u.Id == user.Id && u.Roles.Contains("Reviewer"));
    }

    private async Task<AppUser> SeedUserAsync(string username)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "F",
            LastName = "L",
            UserName = username,
            Email = $"{username}@example.com"
        };

        var result = await _userManager.CreateAsync(user);
        result.Succeeded.Should().BeTrue();
        return user;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
