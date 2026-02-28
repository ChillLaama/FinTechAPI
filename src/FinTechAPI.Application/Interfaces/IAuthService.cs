using FinTechAPI.Application.DTOs;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace FinTechAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(IdentityResult, UserDto)> RegisterAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress);
        Task<string> GenerateJwtToken(User user);
    }
}
