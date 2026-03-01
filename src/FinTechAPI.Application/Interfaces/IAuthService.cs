using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error, UserDto? User)> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    }
}
