using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MRBSystem.Application.Mapper;
using MRBSystem.Application.Services;
using MRBSystem.Contracts.DTOs.RoomEquipmentDtos;
using MRBSystem.Domain.Entities;
using MRBSystem.Persistence.Data;
using MRBSystem.Persistence.Repository;
using MRBSystem.Shared.Exceptions;

namespace MRBSystem.Testing;

public class RoomEquipmentServiceTests
{
    private readonly TestMRBAppDbContext _context;
    private readonly RoomEquipmentService _service;
    private readonly Guid _roomId = Guid.NewGuid();
    private readonly Guid _equipmentId = Guid.NewGuid();

    public RoomEquipmentServiceTests()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MRBAppDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestMRBAppDbContext(options);
        _context.Database.EnsureCreated();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CustomProfile>(), NullLoggerFactory.Instance);

        _service = new RoomEquipmentService(
            new GenericRepository<RoomEquipment>(_context),
            new GenericRepository<Room>(_context),
            new GenericRepository<Equipment>(_context),
            mapperConfig.CreateMapper());

        SeedRoomAndEquipment();
    }

    [Fact]
    public async Task CreateAsync_ValidRoomAndEquipment_Succeeds()
    {
        var result = await _service.CreateAsync(new CreateRoomEquipmentDto
        {
            RoomId = _roomId,
            EquipmentId = _equipmentId,
            Quantity = 2
        });

        result.Should().NotBeNull();
        result.EquipmentName.Should().Be("Projector");
    }

    [Fact]
    public async Task CreateAsync_NonExistentRoom_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(new CreateRoomEquipmentDto
        {
            RoomId = Guid.NewGuid(),
            EquipmentId = _equipmentId,
            Quantity = 1
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_NonExistentEquipment_ThrowsNotFound()
    {
        var act = () => _service.CreateAsync(new CreateRoomEquipmentDto
        {
            RoomId = _roomId,
            EquipmentId = Guid.NewGuid(),
            Quantity = 1
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateAssignment_ThrowsInvalidOperation()
    {
        await _service.CreateAsync(new CreateRoomEquipmentDto
        {
            RoomId = _roomId,
            EquipmentId = _equipmentId,
            Quantity = 1
        });

        var act = () => _service.CreateAsync(new CreateRoomEquipmentDto
        {
            RoomId = _roomId,
            EquipmentId = _equipmentId,
            Quantity = 3
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    private void SeedRoomAndEquipment()
    {
        var building = new Building { Name = "Test Building", Address = "Test Address" };
        var floor = new Floor { Name = "1-ci mertebe", LevelNumber = 1, Building = building };
        var room = new Room { Id = _roomId, Name = "Test Room", Capacity = 10, Floor = floor };
        var equipment = new Equipment { Id = _equipmentId, Name = "Projector" };

        _context.Rooms.Add(room);
        _context.Equipment.Add(equipment);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
