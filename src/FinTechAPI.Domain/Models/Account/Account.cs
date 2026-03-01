namespace FinTechAPI.Domain.Models
{
    public class Account
    {
        public string Id { get; set; } = string.Empty;          // Firestore document ID
        public string Name { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
    }
}
