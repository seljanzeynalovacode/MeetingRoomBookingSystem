using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.SystemSettingsDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers;

[Route("api/system-settings")]
[ApiController]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _systemSettingsService;
    public SystemSettingsController(ISystemSettingsService systemSettingsService)
    {
        _systemSettingsService = systemSettingsService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get()
    {
        var result = await _systemSettingsService.GetAsync();
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateSystemSettingsDto dto)
    {
        var result = await _systemSettingsService.UpdateAsync(dto);
        return Ok(result);
    }
}