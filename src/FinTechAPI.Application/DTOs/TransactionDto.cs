using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.DTOs
{
    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public TransactionType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AccountId { get; set; } = string.Empty;
    }
}
