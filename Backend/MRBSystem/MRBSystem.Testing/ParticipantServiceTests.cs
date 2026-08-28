using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.ParticipantDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Enums;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Shared.Exceptions;

namespace MRBSystem.Testing;

public class ParticipantServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly IMapper _mapper;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _reservationId = Guid.NewGuid();

    public ParticipantServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        SeedReservation();
    }

    private ParticipantService CreateService(Guid currentUserId, bool isAdmin = false)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(currentUserId);
        currentUser.Setup(x => x.IsInRole("Admin")).Returns(isAdmin);

        return new ParticipantService(
            new GenericRepository<Participant>(_context),
            new GenericRepository<Reservation>(_context),
            currentUser.Object,
            _mapper);
    }

    [Fact]
    public async Task CreateAsync_ByReservationOwner_Succeeds()
    {
        var service = CreateService(_ownerId);

        var result = await service.CreateAsync(new CreateParticipantDto
        {
            ReservationId = _reservationId,
            GuestEmail = "qonaq@example.com",
            GuestName = "Qonaq"
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ByRandomUser_ThrowsUnauthorized()
    {
        var service = CreateService(Guid.NewGuid());

        var act = () => service.CreateAsync(new CreateParticipantDto
        {
            ReservationId = _reservationId,
            GuestEmail = "qonaq@example.com",
            GuestName = "Qonaq"
        });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CreateAsync_ByAdmin_Succeeds()
    {
        var service = CreateService(Guid.NewGuid(), isAdmin: true);

        var result = await service.CreateAsync(new CreateParticipantDto
        {
            ReservationId = _reservationId,
            GuestEmail = "qonaq@example.com",
            GuestName = "Qonaq"
        });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ByInvitedParticipantThemselves_Succeeds()
    {
        var invitedUserId = await SeedInvitedUserAsync("invited1", "invited1@example.com");
        var participant = await SeedParticipantAsync(invitedUserId);

        var service = CreateService(invitedUserId);

        var result = await service.UpdateAsync(participant.Id, new UpdateParticipantDto
        {
            ResponseStatus = "Accepted"
        });

        result.ResponseStatus.Should().Be("Accepted");
    }

    [Fact]
    public async Task UpdateAsync_ByUnrelatedUser_ThrowsUnauthorized()
    {
        var invitedUserId = await SeedInvitedUserAsync("invited2", "invited2@example.com");
        var participant = await SeedParticipantAsync(invitedUserId);

        var service = CreateService(Guid.NewGuid());

        var act = () => service.UpdateAsync(participant.Id, new UpdateParticipantDto
        {
            ResponseStatus = "Accepted"
        });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    private async Task<Guid> SeedInvitedUserAsync(string username, string email)
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(new AppUser
        {
            Id = userId,
            FirstName = "Invited",
            LastName = "User",
            UserName = username,
            Email = email
        });
        await _context.SaveChangesAsync();
        return userId;
    }

    private async Task<Participant> SeedParticipantAsync(Guid invitedUserId)
    {
        var participant = new Participant
        {
            ReservationId = _reservationId,
            UserId = invitedUserId,
            ResponseStatus = ResponseStatus.Invited
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return participant;
    }

    private void SeedReservation()
    {
        var owner = new AppUser
        {
            Id = _ownerId,
            FirstName = "Owner",
            LastName = "User",
            UserName = "owner",
            Email = "owner@example.com"
        };

        var building = new Building { Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Name = "1-ci mertebe", LevelNumber = 1, Building = building };
        var room = new Room { Name = "Test Room", Capacity = 10, Floor = floor };

        var reservation = new Reservation
        {
            Id = _reservationId,
            Title = "Test Meeting",
            Room = room,
            ReservedByUserId = _ownerId,
            StartDate = DateTime.UtcNow.AddHours(1),
            EndDate = DateTime.UtcNow.AddHours(2)
        };

        _context.Users.Add(owner);
        _context.Reservations.Add(reservation);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
