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
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MRBSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IGenericRepository<RefreshToken> _refreshTokenRepository;
    private readonly IGenericRepository<Reservation> _reservationRepository;
    private readonly IGenericRepository<Comment> _commentRepository;
    private readonly IGenericRepository<Like> _likeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<AppRole> roleManager,
        IGenericRepository<RefreshToken> refreshTokenRepository,
        IGenericRepository<Reservation> reservationRepository,
        IGenericRepository<Comment> commentRepository,
        IGenericRepository<Like> likeRepository,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IMapper mapper,
        ILogger<AuthService> logger,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _refreshTokenRepository = refreshTokenRepository;
        _reservationRepository = reservationRepository;
        _commentRepository = commentRepository;
        _likeRepository = likeRepository;
        _currentUser = currentUser;
        _configuration = configuration;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
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
            return true;
        }, IsolationLevel.ReadCommitted);

        _logger.LogInformation("User registered successfully with email: {Email}", dto.Email);

        try
        {
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var subject = "Email Ünvanının Təsdiqlənməsi";
            var body = $"Salam {user.FirstName},\n\nQeydiyyatınızı tamamlamaq üçün aşağıdakı kodu istifadə edin:\n\n{confirmationToken}\n\nBu tələbi siz etməmisinizsə, bu mesajı nəzərə almayın.";

            await _emailService.SendEmailAsync(user.Email!, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email confirmation to {Email} after successful registration.", dto.Email);
        }

        var response = _mapper.Map<UserResponseDto>(user);
        response.Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return response;
    }

    public async Task<LoginResponseDto> AdminLoginAsync(LoginDto dto)
    {
        const string genericError = "İstifadəçi adı və ya şifrə yanlışdır.";
        const string lockedOutError = "Hesab müvəqqəti bloklanıb. Zəhmət olmasa bir az sonra yenidən cəhd edin.";

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Admin login attempt failed for non-existent email: {Email}", dto.Email);
            throw new UnauthorizedAccessException(genericError);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Admin login blocked - account is locked out: {Email}", dto.Email);
            throw new UnauthorizedAccessException(lockedOutError);
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (!passwordCheck.Succeeded)
        {
            if (passwordCheck.IsNotAllowed)
            {
                _logger.LogWarning("Admin login blocked - email not confirmed: {Email}", dto.Email);
                throw new UnauthorizedAccessException("Email ünvanınız təsdiqlənməyib. Zəhmət olmasa email-inizi yoxlayın.");
            }

            if (passwordCheck.IsLockedOut)
            {
                _logger.LogWarning("Admin account locked out after too many failed attempts: {Email}", dto.Email);
                throw new UnauthorizedAccessException(lockedOutError);
            }

            _logger.LogWarning("Admin login attempt failed for email: {Email}. Invalid password.", dto.Email);
            throw new UnauthorizedAccessException(genericError);
        }

        if (!await _userManager.IsInRoleAsync(user, "Admin"))

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            _logger.LogWarning("User {Email} attempted to access Admin login without Admin role.", dto.Email);
            throw new UnauthorizedAccessException(genericError);
        }

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        var subject = "Admin Panel - İki Mərhələli Təsdiq Kodu";
        var body = $"Salam {user.FirstName},\n\nSizin Admin panelə giriş üçün təsdiq kodunuz: {code}\n\nBu kodun etibarlılıq müddəti qısadır.";

        try
        {
            await _emailService.SendEmailAsync(user.Email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA code email to {Email}", user.Email);
            throw new InvalidOperationException("Təsdiq kodu göndərilə bilmədi. Zəhmət olmasa bir az sonra yenidən cəhd edin.");
        }

        _logger.LogInformation("2FA Code generated and sent to email: {Email}", user.Email);

        return new LoginResponseDto
        {
            Requires2FA = true,
            Message = "6 rəqəmli təsdiq kodu e-poçt ünvanınıza göndərildi."
        };
    }

    public async Task<TokenResponseDto> VerifyAdmin2FAAsync(Verify2FADto dto)
    {
        const string invalidError = "Daxil etdiyiniz təsdiq kodu yanlışdır və ya vaxtı bitib.";
        const string lockedOutError = "Hesab müvəqqəti bloklanıb. Zəhmət olmasa bir az sonra yenidən cəhd edin.";

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            throw new UnauthorizedAccessException("İstifadəçi tapılmadı və ya hüququnuz yoxdur.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("2FA verification blocked - account is locked out: {Email}", dto.Email);
            throw new UnauthorizedAccessException(lockedOutError);
        }

        var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", dto.Code);

        if (!isCodeValid)
        {
            var failResult = await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Admin account locked out after too many failed 2FA attempts: {Email}", dto.Email);
                throw new UnauthorizedAccessException(lockedOutError);
            }

            _logger.LogWarning("Invalid 2FA code attempt for {Email}", dto.Email);
            throw new UnauthorizedAccessException(invalidError);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Admin {Email} successfully verified 2FA and logged in.", dto.Email);
        return await GenerateTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto> UserLoginAsync(LoginDto dto)
    {
        const string genericError = "İstifadəçi adı və ya şifrə yanlışdır.";
        const string lockedOutError = "Hesab müvəqqəti bloklanıb. Zəhmət olmasa bir az sonra yenidən cəhd edin.";

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            _logger.LogWarning("User login attempt failed for non-existent email: {Email}", dto.Email);
            throw new UnauthorizedAccessException(genericError);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("User login blocked - account is locked out: {Email}", dto.Email);
            throw new UnauthorizedAccessException(lockedOutError);
        }

        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

        if (!passwordCheck.Succeeded)
        {
            if (passwordCheck.IsNotAllowed)
            {
                _logger.LogWarning("User login blocked - email not confirmed: {Email}", dto.Email);
                throw new UnauthorizedAccessException("Email ünvanınız təsdiqlənməyib. Zəhmət olmasa email-inizi yoxlayın.");
            }

            if (passwordCheck.IsLockedOut)
            {
                _logger.LogWarning("User account locked out after too many failed attempts: {Email}", dto.Email);
                throw new UnauthorizedAccessException(lockedOutError);
            }

            _logger.LogWarning("User login attempt failed for email: {Email}. Invalid password.", dto.Email);
            throw new UnauthorizedAccessException(genericError);
        }

        if (!await _userManager.IsInRoleAsync(user, "User"))

            if (!await _userManager.IsInRoleAsync(user, "User"))
        {
            _logger.LogWarning("User {Email} attempted to access User login without User role.", dto.Email);
            throw new UnauthorizedAccessException(genericError);
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
    public async Task DeleteUserAsync(Guid userId)
    {
        if (userId == _currentUser.UserId)
        {
            _logger.LogWarning("Admin {UserId} attempted to delete their own account.", userId);
            throw new InvalidOperationException("Öz hesabınızı silə bilməzsiniz.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Attempt to delete a non-existent user with ID: {UserId}", userId);
            throw new NotFoundException($"User with id {userId} not found.");
        }

        var hasReservations = await _reservationRepository.GetQueryable()
            .AnyAsync(r => r.ReservedByUserId == userId);
        if (hasReservations)
        {
            throw new InvalidOperationException(
                "Bu istifadəçinin mövcud rezervasiyaları var, silinə bilməz.");
        }

        var hasComments = await _commentRepository.GetQueryable()
            .AnyAsync(c => c.UserId == userId);
        if (hasComments)
        {
            throw new InvalidOperationException(
                "Bu istifadəçinin mövcud şərhləri var, silinə bilməz.");
        }

        var hasLikes = await _likeRepository.GetQueryable()
            .AnyAsync(l => l.UserId == userId);
        if (hasLikes)
        {
            throw new InvalidOperationException(
                "Bu istifadəçinin mövcud bəyənmələri var, silinə bilməz.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete user {UserId}. Errors: {Errors}", userId, errors);
            throw new InvalidOperationException(errors);
        }

        _logger.LogInformation("User {UserId} deleted successfully.", userId);
    }
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var subject = "Şifrənin Bərpası";
            var body = $"Salam {user.FirstName},\n\nŞifrənizi bərpa etmək üçün aşağıdakı kodu istifadə edin:\n\n{token}\n\nBu tələbi siz etməmisinizsə, bu mesajı nəzərə almayın.";

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            _logger.LogInformation("Password reset token generated and sent to email: {Email}", dto.Email);
        }
        else
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        const string genericError = "Şifrə bərpası uğursuz oldu. Kod yanlış, vaxtı bitib və ya email yanlışdır.";

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", dto.Email);
            throw new InvalidOperationException(genericError);
        }

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for email: {Email}. Errors: {Errors}",
                dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(genericError);
        }

        _logger.LogInformation("Password successfully reset for email: {Email}", dto.Email);
    }
    public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        const string genericError = "Email təsdiqlənmə kodu yanlışdır, vaxtı bitib və ya email yanlışdır.";

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            _logger.LogWarning("Email confirmation attempted for non-existent email: {Email}", dto.Email);
            throw new InvalidOperationException(genericError);
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email confirmation attempted for already-confirmed email: {Email}", dto.Email);
            return;
        }

        var result = await _userManager.ConfirmEmailAsync(user, dto.Token);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Email confirmation failed for email: {Email}. Errors: {Errors}",
                dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException(genericError);
        }

        _logger.LogInformation("Email successfully confirmed for: {Email}", dto.Email);
    }

    public async Task ResendConfirmationEmailAsync(ResendConfirmationEmailDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is not null && !user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var subject = "Email Ünvanının Təsdiqlənməsi";
            var body = $"Salam {user.FirstName},\n\nQeydiyyatınızı tamamlamaq üçün aşağıdakı kodu istifadə edin:\n\n{token}\n\nBu tələbi siz etməmisinizsə, bu mesajı nəzərə almayın.";

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            _logger.LogInformation("Email confirmation resent to: {Email}", dto.Email);
        }
        else
        {
            _logger.LogWarning("Email confirmation resend requested for non-existent or already-confirmed email: {Email}", dto.Email);
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

        var newTokenResponse = await GenerateTokenResponseAsync(storedToken.User);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = HashToken(newTokenResponse.RefreshToken);
        await _refreshTokenRepository.UpdateAsync(storedToken);

        _logger.LogInformation("Refresh token successfully used and replaced for user ID: {UserId}", storedToken.UserId);
        return newTokenResponse;
    }

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

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        var signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );
        _logger.LogInformation("Generating access token for user ID: {UserId} with roles: {Roles}", user.Id, string.Join(", ", roles));
        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: signingCredentials
        );
        _logger.LogInformation("Access token generated for user ID: {UserId}", user.Id);

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