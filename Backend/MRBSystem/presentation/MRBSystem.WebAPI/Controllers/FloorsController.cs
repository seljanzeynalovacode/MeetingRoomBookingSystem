using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.FloorDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FloorsController : ControllerBase
{
    private readonly IFloorService _floorService;
    public FloorsController(IFloorService floorService)
    {
        _floorService = floorService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetFloors()
    {
        var floors = await _floorService.GetAllAsync();
        return Ok(floors);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _floorService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFloor(Guid id)
    {
        await _floorService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFloor(Guid id, [FromBody] UpdateFloorDto dto)
    {
        var result = await _floorService.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFloor([FromBody] CreateFloorDto dto)
    {
        var result = await _floorService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
