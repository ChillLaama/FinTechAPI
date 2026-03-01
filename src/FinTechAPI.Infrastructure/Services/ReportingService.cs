using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Transaction = FinTechAPI.Domain.Models.Transaction;
using Currency = FinTechAPI.Domain.Models.Currency;
using TransactionType = FinTechAPI.Domain.Models.TransactionType;

namespace FinTechAPI.Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly FirestoreProvider _firestore;

        public ReportingService(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByTypeAsync(TransactionType transactionType, string userId)
        {
            var snapshot = await _firestore.Transactions
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("type", (int)transactionType)
                .OrderByDescending("transactionDate")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(doc => ToTransaction(doc.ConvertTo<TransactionDocument>()));
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = Timestamp.FromDateTime(startDate.ToUniversalTime());
            var end   = Timestamp.FromDateTime(endDate.ToUniversalTime());

            var snapshot = await _firestore.Transactions
                .WhereGreaterThanOrEqualTo("transactionDate", start)
                .WhereLessThanOrEqualTo("transactionDate", end)
                .OrderByDescending("transactionDate")
                .GetSnapshotAsync();

            return snapshot.Documents.Select(doc => ToTransaction(doc.ConvertTo<TransactionDocument>()));
        }

        public decimal CalculateTotalAmount(IEnumerable<Transaction> transactions)
            => transactions.Sum(t => t.Amount);

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
    }
}
