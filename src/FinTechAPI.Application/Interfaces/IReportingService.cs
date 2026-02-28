using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface IReportingService
    {
        Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(TransactionType transactionType, string userId);
        Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        decimal CalculateTotalAmount(IEnumerable<Transaction> transactions);
    }
}
