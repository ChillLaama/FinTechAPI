using System.ComponentModel.DataAnnotations;

namespace FinTechAPI.Application.DTOs
{
    public class CreatePayoutDto
    {
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "usd";

        [StringLength(500)]
        public string? Description { get; set; }

        public string? StripeAccountId { get; set; }

        [StringLength(200)]
        public string? ExternalReference { get; set; }
    }
}

