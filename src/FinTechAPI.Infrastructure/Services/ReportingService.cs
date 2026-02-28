using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly FinTechDbContext _context;

        public ReportingService(FinTechDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(TransactionType transactionType, string userId)
        {
            return await _context.Transactions
                .Where(t => t.Type == transactionType && t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public decimal CalculateTotalAmount(IEnumerable<Transaction> transactions)
        {
            return transactions.Sum(t => t.Amount);
        }
    }
}
