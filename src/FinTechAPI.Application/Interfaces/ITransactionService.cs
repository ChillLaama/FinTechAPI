using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetTransactionsAsync(string userId);
        Task<Transaction?> GetTransactionByIdAsync(string transactionId, string userId);
        Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(string accountId, string userId);
        Task<Transaction?> CreateTransactionAsync(Transaction transaction, string userId);
        Task<Transaction?> UpdateTransactionAsync(string transactionId, Transaction transactionDetails, string userId);
        Task<bool> DeleteTransactionAsync(string transactionId, string userId);
        Task<bool> TransactionExistsAsync(string transactionId, string userId);
    }
}
