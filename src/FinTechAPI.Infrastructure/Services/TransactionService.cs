using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly FinTechDbContext _context;

        public TransactionService(FinTechDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(string userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId, string userId)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId, string userId)
        {
            var accountExists = await _context.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId);
            if (!accountExists)
                return Enumerable.Empty<Transaction>();

            return await _context.Transactions
                .Where(t => t.AccountId == accountId && t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<Transaction?> CreateTransactionAsync(Transaction transaction, string userId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == transaction.AccountId && a.UserId == userId);

            if (account == null)
                return null;

            transaction.UserId = userId;
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.Id = 0;

            _context.Transactions.Add(transaction);

            if (transaction.Type == TransactionType.Income)
                account.Balance += transaction.Amount;
            else if (transaction.Type == TransactionType.Expense)
                account.Balance -= transaction.Amount;

            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<Transaction?> UpdateTransactionAsync(int transactionId, Transaction transactionDetails, string userId)
        {
            var existingTransaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (existingTransaction == null)
                return null;

            var account = existingTransaction.Account;
            if (account == null)
                return null;

            if (existingTransaction.AccountId != transactionDetails.AccountId)
                return null;

            // Revert previous effect
            if (existingTransaction.Type == TransactionType.Income)
                account.Balance -= existingTransaction.Amount;
            else if (existingTransaction.Type == TransactionType.Expense)
                account.Balance += existingTransaction.Amount;

            // Apply new effect
            if (transactionDetails.Type == TransactionType.Income)
                account.Balance += transactionDetails.Amount;
            else if (transactionDetails.Type == TransactionType.Expense)
                account.Balance -= transactionDetails.Amount;

            account.UpdatedAt = DateTime.UtcNow;

            existingTransaction.Amount = transactionDetails.Amount;
            existingTransaction.Type = transactionDetails.Type;
            existingTransaction.Description = transactionDetails.Description;
            existingTransaction.TransactionDate = transactionDetails.TransactionDate;
            existingTransaction.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransactionExistsAsync(transactionId, userId))
                    return null;
                throw;
            }

            return existingTransaction;
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId, string userId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

            if (transaction == null)
                return false;

            var account = transaction.Account;
            if (account == null)
                return false;

            if (transaction.Type == TransactionType.Income)
                account.Balance -= transaction.Amount;
            else if (transaction.Type == TransactionType.Expense)
                account.Balance += transaction.Amount;

            account.UpdatedAt = DateTime.UtcNow;
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TransactionExistsAsync(int transactionId, string userId)
        {
            return await _context.Transactions.AnyAsync(e => e.Id == transactionId && e.UserId == userId);
        }
    }
}
