using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        private readonly IRoleService _roleService;

        public AuthsController(IAuthService authService, IRoleService roleService)
        {
            _authService = authService;
            _roleService = roleService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(result);
        }

        [HttpPost("admin/login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginDto dto)
        {
            var result = await _authService.AdminLoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("admin/login-2fa")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> VerifyAdmin2FA([FromBody] Verify2FADto dto)
        {
            var result = await _authService.VerifyAdmin2FAAsync(dto);
            return Ok(result);
        }

        [HttpPost("user/login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> UserLogin([FromBody] LoginDto dto)
        {
            var result = await _authService.UserLoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }
        [HttpPost("confirm-email")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            await _authService.ConfirmEmailAsync(dto);
            return NoContent();
        }

        [HttpPost("resend-confirmation-email")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailDto dto)
        {
            await _authService.ResendConfirmationEmailAsync(dto);
            return Ok(new { message = "Əgər bu email mövcuddursa və təsdiqlənməyibsə, yeni kod göndərildi." });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            await _authService.LogoutAsync(userId);
            return NoContent();
        }
        [HttpPost("forgot-password")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Əgər bu email mövcuddursa, şifrə bərpa kodu göndərildi." });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return NoContent();
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var result = await _authService.UpdateProfileAsync(dto);
            return Ok(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _authService.ChangePasswordAsync(dto);
            return NoContent();
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleService.GetAllRolesAsync();
            return Ok(result);
        }

        [HttpGet("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            return Ok(result);
        }

        [HttpPost("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] CreateAppRoleDto dto)
        {
            var result = await _roleService.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetRoleById), new { id = result.Id }, result);
        }

        [HttpPut("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateAppRoleDto dto)
        {
            var result = await _roleService.UpdateRoleAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("roles/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            await _roleService.DeleteRoleAsync(id);
            return NoContent();
        }

        [HttpGet("users/{userId:guid}/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserRoles(Guid userId)
        {
            var result = await _roleService.GetUserRolesAsync(userId);
            return Ok(result);
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsersWithRoles()
        {
            var result = await _roleService.GetAllUsersWithRolesAsync();
            return Ok(result);
        }

        [HttpPost("users/{userId:guid}/roles/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(Guid userId, string roleName)
        {
            await _roleService.AssignRoleToUserAsync(userId, roleName);
            return NoContent();
        }

        [HttpDelete("users/{userId:guid}/roles/{roleName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole(Guid userId, string roleName)
        {
            await _roleService.RemoveRoleFromUserAsync(userId, roleName);
            return NoContent();
        }

        [HttpDelete("users/{userId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            await _authService.DeleteUserAsync(userId);
            return NoContent();
        }
    }
}