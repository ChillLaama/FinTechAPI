using AutoMapper;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly FirestoreProvider _firestore;
        private readonly IMapper _mapper;

        public AccountService(FirestoreProvider firestore, IMapper mapper)
        {
            _firestore = firestore;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId)
        {
            var snapshot = await _firestore.Accounts
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var a = doc.ConvertTo<AccountDocument>();
                return _mapper.Map<AccountDto>(ToAccount(a));
            });
        }

        public async Task<Account?> GetAccountByIdAsync(string accountId, string userId)
        {
            var snapshot = await _firestore.Accounts.Document(accountId).GetSnapshotAsync();
            if (!snapshot.Exists) return null;
            var doc = snapshot.ConvertTo<AccountDocument>();
            return doc.UserId != userId ? null : ToAccount(doc);
        }

        public async Task<Account> CreateAccountAsync(Account account, string userId)
        {
            account.UserId    = userId;
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            var docRef = _firestore.Accounts.Document();
            account.Id = docRef.Id;
            await docRef.SetAsync(ToDocument(account));
            return account;
        }

        public async Task<Account?> UpdateAccountAsync(string accountId, Account accountDetails, string userId)
        {
            var docRef   = _firestore.Accounts.Document(accountId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var existing = snapshot.ConvertTo<AccountDocument>();
            if (existing.UserId != userId) return null;

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                ["name"]        = accountDetails.Name,
                ["accountType"] = (int)accountDetails.AccountType,
                ["currency"]    = (int)accountDetails.Currency,
                ["updatedAt"]   = Timestamp.GetCurrentTimestamp()
            });

            existing.Name        = accountDetails.Name;
            existing.AccountType = (int)accountDetails.AccountType;
            existing.Currency    = (int)accountDetails.Currency;
            return ToAccount(existing);
        }

        public async Task<bool> DeleteAccountAsync(string accountId, string userId)
        {
            var snapshot = await _firestore.Accounts.Document(accountId).GetSnapshotAsync();
            if (!snapshot.Exists) return false;
            var doc = snapshot.ConvertTo<AccountDocument>();
            if (doc.UserId != userId) return false;
            await _firestore.Accounts.Document(accountId).DeleteAsync();
            return true;
        }

        public async Task<bool> AccountExistsAsync(string accountId, string userId)
        {
            var snapshot = await _firestore.Accounts.Document(accountId).GetSnapshotAsync();
            if (!snapshot.Exists) return false;
            var doc = snapshot.ConvertTo<AccountDocument>();
            return doc.UserId == userId;
        }

        private static Account ToAccount(AccountDocument d) => new()
        {
            Id          = d.Id,
            Name        = d.Name,
            AccountType = (AccountType)d.AccountType,
            Balance     = (decimal)d.Balance,
            Currency    = (Currency)d.Currency,
            UserId      = d.UserId,
            CreatedAt   = d.CreatedAt.ToDateTime(),
            UpdatedAt   = d.UpdatedAt.ToDateTime()
        };

        private static AccountDocument ToDocument(Account a) => new()
        {
            Id          = a.Id,
            Name        = a.Name,
            AccountType = (int)a.AccountType,
            Balance     = (double)a.Balance,
            Currency    = (int)a.Currency,
            UserId      = a.UserId,
            CreatedAt   = Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime()),
            UpdatedAt   = Timestamp.FromDateTime(a.UpdatedAt.ToUniversalTime())
        };
    }
}
