using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.BuildingWorkingHourDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/buildings/{buildingId:guid}/working-hours")]
    [ApiController]
    public class WorkingHoursController : ControllerBase
    {
        private readonly IWorkingHourService _workingHourService;
        public WorkingHoursController(IWorkingHourService workingHourService)
        {
            _workingHourService = workingHourService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetByBuilding(Guid buildingId)
        {
            var result = await _workingHourService.GetByBuildingIdAsync(buildingId);
            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateForBuilding(Guid buildingId, [FromBody] List<UpdateWorkingHourDto> dto)
        {
            var result = await _workingHourService.UpdateForBuildingAsync(buildingId, dto);
            return Ok(result);
        }
    }
}
