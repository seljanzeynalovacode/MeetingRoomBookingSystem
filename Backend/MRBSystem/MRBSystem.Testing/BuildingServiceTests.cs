using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.BuildingDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Tests;

namespace MRBSystem.Testing;

public class BuildingServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly BuildingService _service;

    public BuildingServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new BuildingService(
      new GenericRepository<Building>(_context),
      new WorkingHourService(
          new GenericRepository<BuildingWorkingHour>(_context),
          new GenericRepository<Building>(_context),
          new FakeUnitOfWork(),
          mapperConfig.CreateMapper(),
          NullLogger<WorkingHourService>.Instance),
      mapperConfig.CreateMapper());
    }

    [Fact]
    public async Task CreateAsync_ValidData_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateBuildingDto
        {
            Name = "Main Office",
            Address = "Baku, Azerbaijan"
        });

        result.Should().NotBeNull();
        result.Name.Should().Be("Main Office");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsNotFound()
    {
        var act = () => _service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ThrowsNotFound()
    {
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateBuildingDto
        {
            Name = "Updated",
            Address = "Updated Address"
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

}
