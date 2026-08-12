using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IGenericRepository<RefreshToken> refreshTokenRepository,
        IConfiguration configuration,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
        _mapper = mapper;
    }
    public async Task<UserResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            throw new ArgumentException("Password and ConfirmPassword do not match.");
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
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
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        const string defaultRole = "User";
        if (!await _roleManager.RoleExistsAsync(defaultRole))
        {
            await _roleManager.CreateAsync(new AppRole { Name = defaultRole });
        }

        await _userManager.AddToRoleAsync(user, defaultRole);

        var response = _mapper.Map<UserResponseDto>(user);
        response.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return response;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) 
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid) {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

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
    }


    public async Task<ResponseAppRoleDto> CreateRoleAsync(CreateAppRoleDto dto)
    {
        if (await _roleManager.RoleExistsAsync(dto.Name))
        {
            throw new InvalidOperationException($"Role '{dto.Name}' already exists.");
        }

        var role = _mapper.Map<AppRole>(dto);
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task<ResponseAppRoleDto> UpdateRoleAsync(Guid id, UpdateAppRoleDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            throw new NotFoundException($"Role with id {id} not found.");
        }

        role.Name = dto.Name;
        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task DeleteRoleAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) {
            throw new NotFoundException($"Role with id {id} not found.");
        }

        await _roleManager.DeleteAsync(role);
    }

    public async Task<ResponseAppRoleDto> GetRoleByIdAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            throw new NotFoundException($"Role with id {id} not found.");
        }

        return _mapper.Map<ResponseAppRoleDto>(role);
    }

    public async Task<ICollection<ResponseAppRoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return _mapper.Map<ICollection<ResponseAppRoleDto>>(roles);
    }

   

    public async Task AssignRoleToUserAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) 
        {
            throw new NotFoundException($"User with id {userId} not found.");
        }

        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            throw new NotFoundException($"Role '{roleName}' not found.");
        }

        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return; 
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task RemoveRoleFromUserAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) {
            throw new NotFoundException($"User with id {userId} not found.");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _refreshTokenRepository.GetQueryable()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (storedToken.RevokedAt is not null)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        // Kohne token-i revoke edir  ve yenisini yaradir
        var newTokenResponse = await GenerateTokenResponseAsync(storedToken.User);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = HashToken(newTokenResponse.RefreshToken);
        await _refreshTokenRepository.UpdateAsync(storedToken);

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

        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!);

        // Butun bu hissələri bir yerə yığıb, əsl JWT token-i yaradırıq
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],        
            audience: _configuration["Jwt:Audience"],      
            claims: claims,                                 
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes), 
            signingCredentials: signingCredentials         
        );

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