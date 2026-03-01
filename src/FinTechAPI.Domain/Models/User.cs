namespace FinTechAPI.Domain.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;          // Firebase Auth UID
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
