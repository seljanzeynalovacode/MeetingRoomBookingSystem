using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AuthDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthsController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthsController(IAuthService authService)
        {
            _authService = authService;
        }

     

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.LogoutAsync(userId);
            return NoContent();
        }


        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _authService.GetAllRolesAsync();
            return Ok(result);
        }

        [HttpGet("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var result = await _authService.GetRoleByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] CreateAppRoleDto dto)
        {
            var result = await _authService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRoleById), new { id = result.Id }, result);
        }

        [HttpPut("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateAppRoleDto dto)
        {
            var result = await _authService.UpdateRoleAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            await _authService.DeleteRoleAsync(id);
            return NoContent();
        }

        // user rol elaqesi
        [HttpGet("users/{userId:guid}/roles")]
        [Authorize]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            var result = await _authService.GetUserRolesAsync(userId);
            return Ok(result);
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithRoles()
        {
            var result = await _authService.GetAllUsersWithRolesAsync();
            return Ok(result);
        }

        [HttpPost("users/{userId:guid}/roles/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(Guid userId, string roleName)
        {
            await _authService.AssignRoleToUserAsync(userId, roleName);
            return NoContent();
        }

        [HttpDelete("users/{userId:guid}/roles/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole(Guid userId, string roleName)
        {
            await _authService.RemoveRoleFromUserAsync(userId, roleName);
            return NoContent();
        }
    }
}
