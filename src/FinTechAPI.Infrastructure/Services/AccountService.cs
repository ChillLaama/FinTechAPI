using AutoMapper;
using AutoMapper.QueryableExtensions;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly FinTechDbContext _context;
        private readonly IMapper _mapper;

        public AccountService(FinTechDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .ProjectTo<AccountDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId, string userId)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
        }

        public async Task<Account> CreateAccountAsync(Account account, string userId)
        {
            account.UserId = userId;
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account?> UpdateAccountAsync(int accountId, Account accountDetails, string userId)
        {
            var existingAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (existingAccount == null)
                return null;

            existingAccount.Name = accountDetails.Name;
            existingAccount.AccountType = accountDetails.AccountType;
            existingAccount.Currency = accountDetails.Currency;
            existingAccount.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AccountExistsAsync(accountId, userId))
                    return null;
                throw;
            }

            return existingAccount;
        }

        public async Task<bool> DeleteAccountAsync(int accountId, string userId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

            if (account == null)
                return false;

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AccountExistsAsync(int accountId, string userId)
        {
            return await _context.Accounts.AnyAsync(e => e.Id == accountId && e.UserId == userId);
        }
    }
}
