using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Tests;

namespace MRBSystem.Testing;

public class WorkingHourServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly WorkingHourService _service;

    public WorkingHourServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new WorkingHourService(
            new GenericRepository<BuildingWorkingHour>(_context),
            new GenericRepository<Building>(_context),
            new FakeUnitOfWork(),
            mapperConfig.CreateMapper(),
            NullLogger<WorkingHourService>.Instance);
    }

    private async Task<Guid> SeedBuildingAsync()
    {
        var building = new Building { Name = "Test Building", Address = "Test Address" };
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();
        return building.Id;
    }

    [Fact]
    public async Task SeedDefaultsAsync_CreatesSevenRows_WeekdaysWorkingWeekendOff()
    {
        var buildingId = await SeedBuildingAsync();

        await _service.SeedDefaultsAsync(buildingId);

        var rows = await _context.BuildingWorkingHours
            .Where(w => w.BuildingId == buildingId)
            .ToListAsync();

        rows.Should().HaveCount(7);
        rows.Where(r => r.DayOfWeek != DayOfWeek.Saturday && r.DayOfWeek != DayOfWeek.Sunday)
            .Should().OnlyContain(r => r.IsWorkingDay && r.StartTime == new TimeSpan(9, 0, 0) && r.EndTime == new TimeSpan(18, 0, 0));
        rows.Where(r => r.DayOfWeek == DayOfWeek.Saturday || r.DayOfWeek == DayOfWeek.Sunday)
            .Should().OnlyContain(r => !r.IsWorkingDay);
    }

    [Fact]
    public async Task GetByBuildingIdAsync_NoExistingRows_SelfHealsAndReturnsSeven()
    {
        var buildingId = await SeedBuildingAsync();

        var result = await _service.GetByBuildingIdAsync(buildingId);

        result.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetByBuildingIdAsync_NonExistentBuilding_ThrowsNotFound()
    {
        await FluentActions.Invoking(() => _service.GetByBuildingIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateForBuildingAsync_ValidData_UpdatesRow()
    {
        var buildingId = await SeedBuildingAsync();
        await _service.SeedDefaultsAsync(buildingId);
        _context.ChangeTracker.Clear();

        var update = new List<UpdateWorkingHourDto>
        {
            new UpdateWorkingHourDto
            {
                DayOfWeek = "Monday",
                IsWorkingDay = true,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(16, 0, 0)
            }
        };

        var result = await _service.UpdateForBuildingAsync(buildingId, update);

        var monday = result.First(r => r.DayOfWeek == "Monday");
        monday.StartTime.Should().Be(new TimeSpan(10, 0, 0));
        monday.EndTime.Should().Be(new TimeSpan(16, 0, 0));
    }

    [Fact]
    public async Task UpdateForBuildingAsync_NonExistentBuilding_ThrowsNotFound()
    {
        var update = new List<UpdateWorkingHourDto>
        {
            new UpdateWorkingHourDto { DayOfWeek = "Monday", IsWorkingDay = true, StartTime = new TimeSpan(9,0,0), EndTime = new TimeSpan(18,0,0) }
        };

        await FluentActions.Invoking(() => _service.UpdateForBuildingAsync(Guid.NewGuid(), update))
            .Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}