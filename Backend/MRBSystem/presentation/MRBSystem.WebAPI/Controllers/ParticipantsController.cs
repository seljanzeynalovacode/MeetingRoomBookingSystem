using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.ParticipantDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParticipantsController : ControllerBase
    {
        private readonly IParticipantService _participantService;

        public ParticipantsController(IParticipantService participantService)
        {
            _participantService = participantService;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _participantService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("by-reservation/{reservationId:guid}")]
        public async Task<IActionResult> GetByReservationId(Guid reservationId)
        {
            var result = await _participantService.GetByReservationIdAsync(reservationId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateParticipantDto dto)
        {
            var result = await _participantService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateParticipantDto dto)
        {
            var result = await _participantService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _participantService.DeleteAsync(id);
            return NoContent();
        }
    }
}
