namespace FinTechAPI.Application.DTOs
{
    public class AccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        // Legacy display field while migrating to Stripe-backed platform balance.
        public decimal Balance { get; set; }
    }
}
