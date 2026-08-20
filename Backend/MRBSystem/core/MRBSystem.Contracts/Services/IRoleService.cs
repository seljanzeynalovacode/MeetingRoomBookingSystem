using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;

namespace MRBSystem.Contracts.Services;

public interface IRoleService
{
    Task<ResponseAppRoleDto> CreateRoleAsync(CreateAppRoleDto dto);
    Task<ResponseAppRoleDto> UpdateRoleAsync(Guid id, UpdateAppRoleDto dto);
    Task DeleteRoleAsync(Guid id);
    Task<ResponseAppRoleDto> GetRoleByIdAsync(Guid id);
    Task<ICollection<ResponseAppRoleDto>> GetAllRolesAsync();

    Task AssignRoleToUserAsync(Guid userId, string roleName);
    Task RemoveRoleFromUserAsync(Guid userId, string roleName);

    Task<List<string>> GetUserRolesAsync(Guid userId);
    Task<ICollection<UserResponseDto>> GetAllUsersWithRolesAsync();
}
