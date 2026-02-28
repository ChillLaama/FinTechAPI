using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTechAPI.Domain.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required] public decimal Amount { get; set; }

        [Required] public Currency Currency { get; set; }

        [Required] public TransactionType Type { get; set; }

        [MaxLength(500)] public string? Description { get; set; }

        [Required] public DateTime TransactionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required] public int AccountId { get; set; }
        [ForeignKey("AccountId")] public virtual Account? Account { get; set; }

        [Required] public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")] public virtual User? User { get; set; }
    }
}
