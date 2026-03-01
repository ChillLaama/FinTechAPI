using FinTechAPI.Application.DTOs;
using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface IAccountService
    {
        Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId);
        Task<Account?> GetAccountByIdAsync(string accountId, string userId);
        Task<Account> CreateAccountAsync(Account account, string userId);
        Task<Account?> UpdateAccountAsync(string accountId, Account accountDetails, string userId);
        Task<bool> DeleteAccountAsync(string accountId, string userId);
        Task<bool> AccountExistsAsync(string accountId, string userId);
    }
}
