using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Exceptions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.EquipmentDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;

namespace MRBSystem.Testing;

public class EquipmentServiceTests : IDisposable
{
    private readonly TestMRBAppDbContext _context;
    private readonly EquipmentService _service;

    public EquipmentServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new EquipmentService(
     new GenericRepository<Equipment>(_context),
     new GenericRepository<RoomEquipment>(_context),
     mapperConfig.CreateMapper());
    }

    [Fact]
    public async Task CreateAsync_ValidData_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateEquipmentDto { Name = "Whiteboard" });

        result.Should().NotBeNull();
        result.Name.Should().Be("Whiteboard");
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
        var act = () => _service.UpdateAsync(Guid.NewGuid(), new UpdateEquipmentDto { Name = "Updated" });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
