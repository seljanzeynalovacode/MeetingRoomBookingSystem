using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Shared.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Contracts.DTOs.FloorDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Application.Services;

namespace MRBSystem.Testing;

public class FloorServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly FloorService _service;
    private readonly Guid _buildingId = Guid.NewGuid();

    public FloorServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new FloorService(
            new GenericRepository<Floor>(_context),
            new GenericRepository<Building>(_context),
            mapperConfig.CreateMapper());

        _context.Buildings.Add(new Building { Id = _buildingId, Name = "Test Building", Address = "Test Address" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ValidBuilding_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateFloorDto
        {
            Name = "1-ci mertebe",
            LevelNumber = 1,
            BuildingId = _buildingId
        });

        result.Should().NotBeNull();
        result.BuildingName.Should().Be("Test Building");
    }

    [Fact]
    public async Task CreateAsync_NonExistentBuilding_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(new CreateFloorDto
        {
            Name = "1-ci mertebe",
            LevelNumber = 1,
            BuildingId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_NonExistentBuilding_ThrowsNotFound()
    {
        var floor = await _service.CreateAsync(new CreateFloorDto
        {
            Name = "1-ci mertebe",
            LevelNumber = 1,
            BuildingId = _buildingId
        });

        var act = () => _service.UpdateAsync(floor.Id, new UpdateFloorDto
        {
            Name = "2-ci mertebe",
            LevelNumber = 2,
            BuildingId = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
