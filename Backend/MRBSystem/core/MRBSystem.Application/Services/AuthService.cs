using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MRBSystem.Application.Exceptions;
using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.AuthDtos;
using MRBSystem.Contracts.Services;
using MRBSystem.Domain.Entities;
using MRBSystem.Domain.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MRBSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IGenericRepository<RefreshToken> refreshTokenRepository,
        IConfiguration configuration,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<UserResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            _logger.LogWarning("Password and ConfirmPassword do not match for email: {Email}", dto.Email);
            throw new ArgumentException("Password and ConfirmPassword do not match.");
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Attempt to register with an existing email: {Email}", dto.Email);
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new AppUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            UserName = dto.Username,
            Email = dto.Email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("User creation failed for email: {Email}. Errors: {Errors}", dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        const string defaultRole = "User";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new AppRole { Name = defaultRole });
        }

        await _userManager.AddToRoleAsync(user, defaultRole);
        _logger.LogInformation("User registered successfully with email: {Email}", dto.Email);

        var response = _mapper.Map<UserResponseDto>(user);
        response.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return response;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) 
        {
            _logger.LogWarning("Login attempt failed for non-existent email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid) {
            _logger.LogWarning("Login attempt failed for email: {Email}. Invalid password.", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("User logged in successfully with email: {Email}", dto.Email);
        return await GenerateTokenResponseAsync(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var activeTokens = await _refreshTokenRepository.GetQueryable()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(token);
        }
        _logger.LogInformation("User with ID: {UserId} logged out and all active refresh tokens revoked.", userId);
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
        if (role == null) {
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

    public async Task<object> GetAllUsersWithRolesAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.UserName,
                user.Email,
                Roles = roles
            });
        }

        _logger.LogInformation("Retrieved all users with their roles. Total count: {Count}", users.Count);
        return userList;
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
    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _refreshTokenRepository.GetQueryable()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (storedToken == null)
        {
            _logger.LogWarning("Invalid refresh token attempt.");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt is not null)
        {
            _logger.LogWarning("Attempt to use a revoked refresh token for user ID: {UserId}", storedToken.UserId);
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Attempt to use an expired refresh token for user ID: {UserId}", storedToken.UserId);
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        // Kohne token-i revoke edir  ve yenisini yaradir
        var newTokenResponse = await GenerateTokenResponseAsync(storedToken.User);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = HashToken(newTokenResponse.RefreshToken);
        await _refreshTokenRepository.UpdateAsync(storedToken);

        _logger.LogInformation("Refresh token successfully used and replaced for user ID: {UserId}", storedToken.UserId);
        return newTokenResponse;
    }
    // ---

    private async Task<TokenResponseDto> GenerateTokenResponseAsync(AppUser user)
    {
        var accessToken = await GenerateAccessTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        var refreshExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        _logger.LogInformation("Refresh token generated and stored for user ID: {UserId}", user.Id);
        var accessExpirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(accessExpirationMinutes)
        };

    }

    private async Task<string> GenerateAccessTokenAsync(AppUser user)
    {
      
        var roles = await _userManager.GetRolesAsync(user);

        //Token-in icinde hansi ne yazacagimizi siyahi kimi qururuq
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),  
        new Claim(ClaimTypes.GivenName, user.FirstName),          
        new Claim(ClaimTypes.Surname, user.LastName)              
    };

        // her rol ucun de ayrica elave edirik
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // tokeni imzalamaq ucun gizli acarimizi hazirlanir
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        // hansi acarla, hansi usulla imzalayacagiq
        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );
        _logger.LogInformation("Generating access token for user ID: {UserId} with roles: {Roles}", user.Id, string.Join(", ", roles));
        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!);

        // Butun bu hissələri bir yerə yığıb, əsl JWT token-i yaradırıq
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],        
            audience: _configuration["Jwt:Audience"],      
            claims: claims,                                 
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes), 
            signingCredentials: signingCredentials         
        );
        _logger.LogInformation("Access token generated for user ID: {UserId}", user.Id);

        //  obyekti, gondermek ucun adi bir stringe cevir
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}