namespace FinTechAPI.Application.DTOs
{
    public class AccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Legacy display field — Stripe platform balance is authoritative.
        [Obsolete("Use platform balance endpoint instead.")]
        public decimal Balance { get; set; }
    }
}

