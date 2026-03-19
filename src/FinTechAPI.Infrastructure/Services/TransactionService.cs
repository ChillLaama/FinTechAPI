using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Transaction = FinTechAPI.Domain.Models.Transaction;
using Currency = FinTechAPI.Domain.Models.Currency;
using TransactionType = FinTechAPI.Domain.Models.TransactionType;
using TransactionStatus = FinTechAPI.Domain.Models.TransactionStatus;

namespace FinTechAPI.Infrastructure.Services
{
    // Non-custodial model: transactions track business lifecycle only.
    // Monetary balances are sourced from Stripe, not from internal account balance mutations.
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

            transaction.UserId = userId;
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            var docRef = _firestore.Transactions.Document();
            transaction.Id = docRef.Id;
            await docRef.SetAsync(ToDocument(transaction));

            return transaction;
        }

        public async Task<Transaction?> UpdateTransactionAsync(string transactionId, Transaction transactionDetails, string userId)
        {
            var txnSnap = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!txnSnap.Exists) return null;
            var existing = txnSnap.ConvertTo<TransactionDocument>();
            if (existing.UserId != userId) return null;
            if (existing.AccountId != transactionDetails.AccountId) return null;

            await _firestore.Transactions.Document(transactionId).UpdateAsync(new Dictionary<string, object>
            {
                ["amount"] = (double)transactionDetails.Amount,
                ["type"] = (int)transactionDetails.Type,
                ["status"] = (int)transactionDetails.Status,
                ["category"] = transactionDetails.Category,
                ["description"] = transactionDetails.Description ?? FieldValue.Delete,
                ["transactionDate"] = Timestamp.FromDateTime(transactionDetails.TransactionDate.ToUniversalTime()),
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            existing.Amount = (double)transactionDetails.Amount;
            existing.Type = (int)transactionDetails.Type;
            existing.Status = (int)transactionDetails.Status;
            existing.Category = transactionDetails.Category;
            existing.Description = transactionDetails.Description;
            return ToTransaction(existing);
        }

        public async Task<Transaction?> UpdateTransactionStatusAsync(string transactionId, TransactionStatus status, string userId)
        {
            var txnSnap = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!txnSnap.Exists) return null;

            var existing = txnSnap.ConvertTo<TransactionDocument>();
            if (existing.UserId != userId) return null;

            var previousStatus = (TransactionStatus)existing.Status;
            if (previousStatus == status)
                return ToTransaction(existing);

            await _firestore.Transactions.Document(transactionId).UpdateAsync(new Dictionary<string, object>
            {
                ["status"] = (int)status,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            existing.Status = (int)status;
            return ToTransaction(existing);
        }

        public async Task<bool> DeleteTransactionAsync(string transactionId, string userId)
        {
            var snapshot = await _firestore.Transactions.Document(transactionId).GetSnapshotAsync();
            if (!snapshot.Exists) return false;
            var doc = snapshot.ConvertTo<TransactionDocument>();
            if (doc.UserId != userId) return false;

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
            Id = d.Id,
            Amount = (decimal)d.Amount,
            Currency = (Currency)d.Currency,
            Type = (TransactionType)d.Type,
            Status = (TransactionStatus)d.Status,
            Category = d.Category,
            Description = d.Description,
            TransactionDate = d.TransactionDate.ToDateTime(),
            AccountId = d.AccountId,
            UserId = d.UserId,
            CreatedAt = d.CreatedAt.ToDateTime(),
            UpdatedAt = d.UpdatedAt.ToDateTime()
        };

        private static TransactionDocument ToDocument(Transaction t) => new()
        {
            Id = t.Id,
            Amount = (double)t.Amount,
            Currency = (int)t.Currency,
            Type = (int)t.Type,
            Status = (int)t.Status,
            Category = t.Category,
            Description = t.Description,
            TransactionDate = Timestamp.FromDateTime(t.TransactionDate.ToUniversalTime()),
            AccountId = t.AccountId,
            UserId = t.UserId,
            CreatedAt = Timestamp.FromDateTime(t.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(t.UpdatedAt.ToUniversalTime())
        };
    }
}
