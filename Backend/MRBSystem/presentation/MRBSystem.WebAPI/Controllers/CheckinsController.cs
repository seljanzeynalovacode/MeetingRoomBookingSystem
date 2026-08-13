using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.CheckinDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckinsController : ControllerBase
    {
        private readonly ICheckinService _checkinService;

        public CheckinsController(ICheckinService checkinService)
        {
            _checkinService = checkinService;
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CreateCheckinDto dto)
        {
            var result = await _checkinService.CheckInAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _checkinService.GetByIdAsync(id);
            return Ok(result);
        }
    }
}
