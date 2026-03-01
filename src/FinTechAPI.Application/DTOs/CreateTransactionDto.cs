using System.ComponentModel.DataAnnotations;
using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.DTOs
{
    public class CreateTransactionDto
    {
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Category { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public string AccountId { get; set; } = string.Empty;
    }
}
