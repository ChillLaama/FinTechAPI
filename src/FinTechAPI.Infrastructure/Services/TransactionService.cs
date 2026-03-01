using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Transaction = FinTechAPI.Domain.Models.Transaction;
using Currency = FinTechAPI.Domain.Models.Currency;
using TransactionType = FinTechAPI.Domain.Models.TransactionType;

namespace FinTechAPI.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly FirestoreProvider _firestore;

        public TransactionService(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(string userId)
        {
            var snapshot = await _firestore.Transactions
                .WhereEqualTo("userId", userId)
                .OrderByDescending("transactionDate")
                .GetSnapshotAsync();
            return snapshot.Documents.Select(doc => ToTransaction(doc.ConvertTo<TransactionDocument>()));
        }

        public async Task<Transaction?> GetTransactionByIdAsync(string transactionId, string userId)
        {
            var snapshot = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!snapshot.Exists) return null;
            var doc = snapshot.ConvertTo<TransactionDocument>();
            return doc.UserId != userId ? null : ToTransaction(doc);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(string accountId, string userId)
        {
            var accountSnap = await _firestore.Accounts.Document(accountId).GetSnapshotAsync();
            if (!accountSnap.Exists) return Enumerable.Empty<Transaction>();
            if (accountSnap.ConvertTo<AccountDocument>().UserId != userId)
                return Enumerable.Empty<Transaction>();

            var snapshot = await _firestore.Transactions
                .WhereEqualTo("accountId", accountId)
                .WhereEqualTo("userId", userId)
                .OrderByDescending("transactionDate")
                .GetSnapshotAsync();
            return snapshot.Documents.Select(doc => ToTransaction(doc.ConvertTo<TransactionDocument>()));
        }

        public async Task<Transaction?> CreateTransactionAsync(Transaction transaction, string userId)
        {
            var accountSnap = await _firestore.Accounts.Document(transaction.AccountId).GetSnapshotAsync();
            if (!accountSnap.Exists) return null;
            var accountDoc = accountSnap.ConvertTo<AccountDocument>();
            if (accountDoc.UserId != userId) return null;

            transaction.UserId    = userId;
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            var docRef = _firestore.Transactions.Document();
            transaction.Id = docRef.Id;
            await docRef.SetAsync(ToDocument(transaction));

            double delta = transaction.Type == TransactionType.Income
                ? (double)transaction.Amount
                : transaction.Type == TransactionType.Expense ? -(double)transaction.Amount : 0;

            if (delta != 0)
            {
                await _firestore.Accounts.Document(transaction.AccountId).UpdateAsync(new Dictionary<string, object>
                {
                    ["balance"]   = accountDoc.Balance + delta,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                });
            }

            return transaction;
        }

        public async Task<Transaction?> UpdateTransactionAsync(string transactionId, Transaction transactionDetails, string userId)
        {
            var txnSnap = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!txnSnap.Exists) return null;
            var existing = txnSnap.ConvertTo<TransactionDocument>();
            if (existing.UserId != userId) return null;
            if (existing.AccountId != transactionDetails.AccountId) return null;

            var accountSnap = await _firestore.Accounts.Document(existing.AccountId).GetSnapshotAsync();
            if (!accountSnap.Exists) return null;
            var accountDoc = accountSnap.ConvertTo<AccountDocument>();

            double revert = existing.Type == (int)TransactionType.Income
                ? -(double)existing.Amount
                : existing.Type == (int)TransactionType.Expense ? existing.Amount : 0;

            double apply = transactionDetails.Type == TransactionType.Income
                ? (double)transactionDetails.Amount
                : transactionDetails.Type == TransactionType.Expense ? -(double)transactionDetails.Amount : 0;

            await _firestore.Transactions.Document(transactionId).UpdateAsync(new Dictionary<string, object>
            {
                ["amount"]          = (double)transactionDetails.Amount,
                ["type"]            = (int)transactionDetails.Type,
                ["description"]     = transactionDetails.Description ?? (object)FieldValue.Delete,
                ["transactionDate"] = Timestamp.FromDateTime(transactionDetails.TransactionDate.ToUniversalTime()),
                ["updatedAt"]       = Timestamp.GetCurrentTimestamp()
            });

            await _firestore.Accounts.Document(existing.AccountId).UpdateAsync(new Dictionary<string, object>
            {
                ["balance"]   = accountDoc.Balance + revert + apply,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            existing.Amount = (double)transactionDetails.Amount;
            existing.Type   = (int)transactionDetails.Type;
            existing.Description = transactionDetails.Description;
            return ToTransaction(existing);
        }

        public async Task<bool> DeleteTransactionAsync(string transactionId, string userId)
        {
            var snapshot = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!snapshot.Exists) return false;
            var doc = snapshot.ConvertTo<TransactionDocument>();
            if (doc.UserId != userId) return false;

            var accountSnap = await _firestore.Accounts.Document(doc.AccountId).GetSnapshotAsync();
            if (accountSnap.Exists)
            {
                var accountDoc = accountSnap.ConvertTo<AccountDocument>();
                double revert = doc.Type == (int)TransactionType.Income
                    ? -(double)doc.Amount
                    : doc.Type == (int)TransactionType.Expense ? doc.Amount : 0;

                await _firestore.Accounts.Document(doc.AccountId).UpdateAsync(new Dictionary<string, object>
                {
                    ["balance"]   = accountDoc.Balance + revert,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                });
            }

            await _firestore.Transactions.Document(transactionId).DeleteAsync();
            return true;
        }

        public async Task<bool> TransactionExistsAsync(string transactionId, string userId)
        {
            var snapshot = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!snapshot.Exists) return false;
            var doc = snapshot.ConvertTo<TransactionDocument>();
            return doc.UserId == userId;
        }

        private static Transaction ToTransaction(TransactionDocument d) => new()
        {
            Id              = d.Id,
            Amount          = (decimal)d.Amount,
            Currency        = (Currency)d.Currency,
            Type            = (TransactionType)d.Type,
            Description     = d.Description,
            TransactionDate = d.TransactionDate.ToDateTime(),
            AccountId       = d.AccountId,
            UserId          = d.UserId,
            CreatedAt       = d.CreatedAt.ToDateTime(),
            UpdatedAt       = d.UpdatedAt.ToDateTime()
        };

        private static TransactionDocument ToDocument(Transaction t) => new()
        {
            Id              = t.Id,
            Amount          = (double)t.Amount,
            Currency        = (int)t.Currency,
            Type            = (int)t.Type,
            Description     = t.Description,
            TransactionDate = Timestamp.FromDateTime(t.TransactionDate.ToUniversalTime()),
            AccountId       = t.AccountId,
            UserId          = t.UserId,
            CreatedAt       = Timestamp.FromDateTime(t.CreatedAt.ToUniversalTime()),
            UpdatedAt       = Timestamp.FromDateTime(t.UpdatedAt.ToUniversalTime())
        };
    }
}
