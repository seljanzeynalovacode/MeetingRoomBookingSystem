using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.AuthDtos;
using MRBSystem.Contracts.Services;

namespace MRBSystem.Application.Services;

public class AuthService : IAuthService
{
    public Task AssignRoleToUserAsync(Guid userId, string roleName)
    {
        throw new NotImplementedException();
    }

    public Task<ResponseAppRoleDto> CreateRoleAsync(CreateAppRoleDto dto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteRoleAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<ICollection<ResponseAppRoleDto>> GetAllRolesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ResponseAppRoleDto> GetRoleByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        throw new NotImplementedException();
    }

    public Task LogoutAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponseDto> RegisterAsync(RegisterDto dto)
    {
        throw new NotImplementedException();
    }

    public Task RemoveRoleFromUserAsync(Guid userId, string roleName)
    {
        throw new NotImplementedException();
    }

    public Task<ResponseAppRoleDto> UpdateRoleAsync(Guid id, UpdateAppRoleDto dto)
    {
        throw new NotImplementedException();
    }
}
