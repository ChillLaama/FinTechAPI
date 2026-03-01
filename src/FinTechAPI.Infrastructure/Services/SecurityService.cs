using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Transaction = FinTechAPI.Domain.Models.Transaction;
using Currency = FinTechAPI.Domain.Models.Currency;
using TransactionType = FinTechAPI.Domain.Models.TransactionType;

namespace FinTechAPI.Infrastructure.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly FirestoreProvider _firestore;

        public SecurityService(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        public IEnumerable<Transaction> DetectAnomalies(decimal threshold)
            => DetectAnomaliesAsync(threshold).GetAwaiter().GetResult();

        public async Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold)
        {
            var snapshot = await _firestore.Transactions
                .WhereGreaterThan("amount", (double)threshold)
                .GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var d = doc.ConvertTo<TransactionDocument>();
                return new Transaction
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
            });
        }
    }
}
