using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;

namespace MRBSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ResponseAppRoleDto> CreateRoleAsync(CreateAppRoleDto dto)
    {
        if (await _roleManager.RoleExistsAsync(dto.Name))
        {
            _logger.LogWarning("Attempt to create an existing role: {RoleName}", dto.Name);
            throw new InvalidOperationException($"Role '{dto.Name}' already exists.");
        }

        var role = _mapper.Map<AppRole>(dto);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            _logger.LogError("Role creation failed for role: {RoleName}. Errors: {Errors}", dto.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        _logger.LogInformation("Role created successfully: {RoleName}", dto.Name);

        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task<ResponseAppRoleDto> UpdateRoleAsync(Guid id, UpdateAppRoleDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            _logger.LogWarning("Attempt to update a non-existent role with ID: {RoleId}", id);
            throw new NotFoundException($"Role with id {id} not found.");
        }

        role.Name = dto.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            _logger.LogError("Role update failed for role ID: {RoleId}. Errors: {Errors}", id, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        _logger.LogInformation("Role updated successfully: {RoleName} (ID: {RoleId})", dto.Name, id);
        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            _logger.LogWarning("Attempt to delete a non-existent role with ID: {RoleId}", id);
            throw new NotFoundException($"Role with id {id} not found.");
        }
        _logger.LogInformation("Role deleted successfully: {RoleName} (ID: {RoleId})", role.Name, id);

        await _roleManager.DeleteAsync(role);
    }

    public async Task<ResponseAppRoleDto> GetRoleByIdAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            _logger.LogWarning("Attempt to retrieve a non-existent role with ID: {RoleId}", id);
            throw new NotFoundException($"Role with id {id} not found.");
        }
        _logger.LogInformation("Role retrieved successfully: {RoleName} (ID: {RoleId})", role.Name, id);
        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task<ICollection<ResponseAppRoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        _logger.LogInformation("Retrieved all roles. Total count: {Count}", roles.Count);
        return _mapper.Map<ICollection<ResponseAppRoleDto>>(roles);
    }

    public async Task AssignRoleToUserAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempt to assign role '{RoleName}' to a non-existent user with ID: {UserId}", roleName, userId);
            throw new NotFoundException($"User with id {userId} not found.");
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            _logger.LogWarning("Attempt to assign a non-existent role '{RoleName}' to user with ID: {UserId}", roleName, userId);
            throw new NotFoundException($"Role '{roleName}' not found.");
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            _logger.LogInformation("User with ID: {UserId} is already assigned to role '{RoleName}'. No action taken.", userId, roleName);
            return;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to assign role '{RoleName}' to user with ID: {UserId}. Errors: {Errors}", roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        _logger.LogInformation("Role '{RoleName}' successfully assigned to user with ID: {UserId}", roleName, userId);
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempt to retrieve roles for a non-existent user with ID: {UserId}", userId);
            throw new NotFoundException($"User with id {userId} not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("Retrieved roles for user ID: {UserId}. Roles: {Roles}", userId, string.Join(", ", roles));

        return roles.ToList();
    }

    public async Task<ICollection<UserResponseDto>> GetAllUsersWithRolesAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var dto = _mapper.Map<UserResponseDto>(user);
            dto.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            result.Add(dto);
        }

        _logger.LogInformation("Retrieved all users with their roles. Total count: {Count}", users.Count);
        return result;
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            _logger.LogWarning("Attempt to remove role '{RoleName}' from a non-existent user with ID: {UserId}", roleName, userId);
            throw new NotFoundException($"User with id {userId} not found.");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to remove role '{RoleName}' from user with ID: {UserId}. Errors: {Errors}", roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
