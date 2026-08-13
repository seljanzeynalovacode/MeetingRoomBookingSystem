using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.RoomEquipmentDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomEquipmentsController : ControllerBase
    {
        private readonly IRoomEquipmentService _roomEquipmentService;

        public RoomEquipmentsController(IRoomEquipmentService roomEquipmentService)
        {
            _roomEquipmentService = roomEquipmentService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roomEquipmentService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _roomEquipmentService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateRoomEquipmentDto dto)
        {
            var result = await _roomEquipmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomEquipmentDto dto)
        {
            var result = await _roomEquipmentService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _roomEquipmentService.DeleteAsync(id);
            return NoContent();
        }
    }
}
