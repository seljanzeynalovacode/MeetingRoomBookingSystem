using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.BuildingDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingService _buildingService;
    public BuildingsController(IBuildingService buildingService)
    {
        _buildingService = buildingService;
    }
   
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBuildings()
    {
        var buildings = await _buildingService.GetAllAsync();
        return Ok(buildings);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _buildingService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBuilding([FromBody] CreateBuildingDto dto)
    {
        var result = await _buildingService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBuilding(Guid id)
    {
        await _buildingService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBuilding(Guid id, [FromBody] UpdateBuildingDto dto)
    {
        var result = await _buildingService.UpdateAsync(id, dto);
        return Ok(result);
    }
}
