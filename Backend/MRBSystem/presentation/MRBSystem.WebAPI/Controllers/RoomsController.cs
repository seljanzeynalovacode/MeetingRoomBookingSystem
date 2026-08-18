using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.RoomDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _roomService.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _roomService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost("search-available")]
    [Authorize]
    public async Task<IActionResult> SearchAvailable([FromBody] RoomAvailabilityRequestDto dto)
    {
        var result = await _roomService.SearchAvailableAsync(dto);
        return Ok(result);
    }

    [HttpGet("{id:guid}/calendar")]
    [Authorize]
    public async Task<IActionResult> GetCalendar(Guid id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _roomService.GetRoomCalendarAsync(id, startDate, endDate);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        await _roomService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomDto dto)
    {
        var result = await _roomService.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
    {
        var result = await _roomService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}