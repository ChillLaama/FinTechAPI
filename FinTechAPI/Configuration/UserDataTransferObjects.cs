// UserDto.cs
namespace FinTechAPI.DTOs
{
    public class RegisterUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    
    public class UserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Expiration { get; set; }
        public UserDto User { get; set; }
        
        public bool Success { get; set; }
    }
}