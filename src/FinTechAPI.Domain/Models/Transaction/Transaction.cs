namespace FinTechAPI.Domain.Models
{
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;          // Firestore document ID
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public TransactionType Type { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string AccountId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
