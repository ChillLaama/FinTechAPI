namespace FinTechAPI.Application.DTOs
{
    public class AccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }
}
