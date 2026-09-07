using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.SystemSettingsDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Tests;

namespace MRBSystem.Testing;

public class SystemSettingsServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly SystemSettingsService _service;

    public SystemSettingsServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new SystemSettingsService(
            new GenericRepository<SystemSettings>(_context),
            new FakeUnitOfWork(),
            mapperConfig.CreateMapper(),
            NullLogger<SystemSettingsService>.Instance);
    }

    [Fact]
    public async Task GetAsync_NoExistingSettings_CreatesAndReturnsDefaults()
    {
        var result = await _service.GetAsync();

        result.MinReservationMinutes.Should().Be(30);
        result.MaxReservationHours.Should().Be(4);
        result.BookingHorizonDays.Should().Be(30);
        result.MaxActiveReservationsPerUser.Should().Be(5);
        result.MaxCalendarRangeDays.Should().Be(90);
    }

    [Fact]
    public async Task GetAsync_CalledTwice_DoesNotDuplicateSettings()
    {
        await _service.GetAsync();
        await _service.GetAsync();

        var count = await _context.SystemSettings.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NoExistingSettings_CreatesNewWithProvidedValues()
    {
        var dto = new UpdateSystemSettingsDto
        {
            MinReservationMinutes = 15,
            MaxReservationHours = 6,
            BufferMinutes = 10,
            BookingHorizonDays = 60,
            IsActiveReservationLimitEnabled = true,
            MaxActiveReservationsPerUser = 3,
            MaxCalendarRangeDays = 120
        };

        var result = await _service.UpdateAsync(null, dto);

        result.MinReservationMinutes.Should().Be(15);
        result.MaxActiveReservationsPerUser.Should().Be(3);
        result.IsActiveReservationLimitEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ExistingSettings_UpdatesInPlaceWithoutDuplicating()
    {
        await _service.GetAsync(); // default qeydi yaradır
        _context.ChangeTracker.Clear(); // real HTTP sorğusunda hər dəfə təzə DbContext olardı — burda əl ilə təqlid edirik

        var dto = new UpdateSystemSettingsDto
        {
            MinReservationMinutes = 45,
            MaxReservationHours = 2,
            BufferMinutes = 5,
            BookingHorizonDays = 14,
            IsActiveReservationLimitEnabled = false,
            MaxActiveReservationsPerUser = 10,
            MaxCalendarRangeDays = 30
        };

        await _service.UpdateAsync(null, dto);

        var count = await _context.SystemSettings.CountAsync();
        count.Should().Be(1);

        var updated = await _context.SystemSettings.FirstAsync();
        updated.MinReservationMinutes.Should().Be(45);
        updated.MaxActiveReservationsPerUser.Should().Be(10);
    }

    [Fact]
    public async Task ResetToGlobalAsync_ExistingOverride_DeletesItAndFallsBackToGlobal()
    {
        var building = new Building { Name = "Test Building", Address = "Test Address" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();
        var buildingId = building.Id;

        var globalDto = new UpdateSystemSettingsDto
        {
            MinReservationMinutes = 30,
            MaxReservationHours = 4,
            BufferMinutes = 0,
            BookingHorizonDays = 30,
            IsActiveReservationLimitEnabled = false,
            MaxActiveReservationsPerUser = 5,
            MaxCalendarRangeDays = 90
        };
        var overrideDto = new UpdateSystemSettingsDto
        {
            MinReservationMinutes = 10,
            MaxReservationHours = 2,
            BufferMinutes = 5,
            BookingHorizonDays = 14,
            IsActiveReservationLimitEnabled = true,
            MaxActiveReservationsPerUser = 2,
            MaxCalendarRangeDays = 30
        };

        await _service.UpdateAsync(null, globalDto);
        await _service.UpdateAsync(buildingId, overrideDto);
        _context.ChangeTracker.Clear();

        await _service.ResetToGlobalAsync(buildingId);
        _context.ChangeTracker.Clear();

        var count = await _context.SystemSettings.CountAsync();
        count.Should().Be(1); // yalnız qlobal sətir qalır

        var result = await _service.GetAsync(buildingId);
        result.BuildingId.Should().BeNull();
        result.MinReservationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task ResetToGlobalAsync_NoExistingOverride_DoesNothing()
    {
        var buildingId = Guid.NewGuid();

        var act = async () => await _service.ResetToGlobalAsync(buildingId);

        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
