using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetTransactionsAsync(string userId);
        Task<Transaction?> GetTransactionByIdAsync(int transactionId, string userId);
        Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId, string userId);
        Task<Transaction?> CreateTransactionAsync(Transaction transaction, string userId);
        Task<Transaction?> UpdateTransactionAsync(int transactionId, Transaction transactionDetails, string userId);
        Task<bool> DeleteTransactionAsync(int transactionId, string userId);
        Task<bool> TransactionExistsAsync(int transactionId, string userId);
    }
}
