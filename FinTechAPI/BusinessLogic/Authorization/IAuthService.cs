using FinTechAPI.DTOs;
using FinTechAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace FinTechAPI.BusinessLogic.Authorization
{
    public interface IAuthService
    {
        Task<(IdentityResult, UserDto)> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress);
        Task<string> GenerateJwtToken(User user);
    }
}