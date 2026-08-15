using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.AuthDtos;

namespace MRBSystem.Contracts.Services;

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(LoginDto dto);
    Task<UserResponseDto> RegisterAsync(RegisterDto dto);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);

    // Rol
    Task<ResponseAppRoleDto> CreateRoleAsync(CreateAppRoleDto dto);
    Task<ResponseAppRoleDto> UpdateRoleAsync(Guid id, UpdateAppRoleDto dto);
    Task DeleteRoleAsync(Guid id);
    Task<ResponseAppRoleDto> GetRoleByIdAsync(Guid id);
    Task<ICollection<ResponseAppRoleDto>> GetAllRolesAsync();

    // Usere rol elaqesi
    Task AssignRoleToUserAsync(Guid userId, string roleName);
    Task RemoveRoleFromUserAsync(Guid userId, string roleName);

    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task<object> GetAllUsersWithRolesAsync();
}
