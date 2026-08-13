using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.RoomBlackoutDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomBlackoutsController : ControllerBase
    {
        private readonly IRoomBlackoutService _roomBlackoutService;

        public RoomBlackoutsController(IRoomBlackoutService roomBlackoutService)
        {
            _roomBlackoutService = roomBlackoutService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roomBlackoutService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _roomBlackoutService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateRoomBlackoutDto dto)
        {
            var result = await _roomBlackoutService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomBlackoutDto dto)
        {
            var result = await _roomBlackoutService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _roomBlackoutService.DeleteAsync(id);
            return NoContent();
        }
    }
}
