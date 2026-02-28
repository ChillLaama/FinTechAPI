using FinTechAPI.Application.DTOs;
using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface IAccountService
    {
        Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId);
        Task<Account?> GetAccountByIdAsync(int accountId, string userId);
        Task<Account> CreateAccountAsync(Account account, string userId);
        Task<Account?> UpdateAccountAsync(int accountId, Account accountDetails, string userId);
        Task<bool> DeleteAccountAsync(int accountId, string userId);
        Task<bool> AccountExistsAsync(int accountId, string userId);
    }
}
