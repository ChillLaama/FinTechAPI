using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, UserDto? User)> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<AuthOperationResultDto> SendPasswordResetEmailAsync(ForgotPasswordDto dto);
        Task<AuthOperationResultDto> ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthOperationResultDto> SendEmailVerificationAsync(string idToken);
        Task<AuthOperationResultDto> VerifyEmailAsync(VerifyEmailDto dto);
    }
}
