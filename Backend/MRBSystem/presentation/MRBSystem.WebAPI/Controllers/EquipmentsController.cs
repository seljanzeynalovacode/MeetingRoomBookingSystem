using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.EquipmentDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentsController : ControllerBase
    {
        private readonly IEquipmentService _equipmentService;
        public EquipmentsController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetEquipments()
        {
            var equipments = await _equipmentService.GetAllAsync();
            return Ok(equipments);
        }
        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _equipmentService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEquipment(Guid id)
        {
            await _equipmentService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEquipment(Guid id, [FromBody] UpdateEquipmentDto dto)
        {
            var result = await _equipmentService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentDto dto)
        {
            var result = await _equipmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
    }
}
