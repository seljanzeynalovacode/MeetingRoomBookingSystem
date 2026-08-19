using MRBSystem.Contracts.DTOs.AppRoleDtos;
using MRBSystem.Contracts.DTOs.AppUserDtos;
using MRBSystem.Contracts.DTOs.AuthDtos;

namespace MRBSystem.Contracts.Services;

public interface IAuthService
{
    Task<LoginResponseDto> AdminLoginAsync(LoginDto dto);
    Task<TokenResponseDto> VerifyAdmin2FAAsync(Verify2FADto dto);
    Task<TokenResponseDto> UserLoginAsync(LoginDto dto);

    Task<UserResponseDto> RegisterAsync(RegisterDto dto);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task ConfirmEmailAsync(ConfirmEmailDto dto);
    Task ResendConfirmationEmailAsync(ResendConfirmationEmailDto dto);

    Task DeleteUserAsync(Guid userId);
}
