using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Services;

namespace FinTechAPI.Tests.BusinessLogic
{
    /// <summary>
    /// Tests for pure business logic in ReportingService that does not require Firestore.
    /// </summary>
    public class ReportingServiceLogicTests
    {
        private readonly ReportingService _service;

        public ReportingServiceLogicTests()
        {
            // FirestoreProvider is only needed for Firestore queries, not for CalculateTotalAmount
            _service = new ReportingService(null!);
        }

        [Fact]
        public void CalculateTotalAmount_ShouldReturnCorrectSum()
        {
            var transactions = new List<Transaction>
            {
                new() { Amount = 100 },
                new() { Amount = 250 },
                new() { Amount = -50 }
            };

            var total = _service.CalculateTotalAmount(transactions);

            Assert.Equal(300m, total);
        }

        [Fact]
        public void CalculateTotalAmount_ShouldReturnZero_ForEmptyList()
        {
            var total = _service.CalculateTotalAmount(Enumerable.Empty<Transaction>());

            Assert.Equal(0m, total);
        }

        [Fact]
        public void CalculateTotalAmount_ShouldHandleSingleTransaction()
        {
            var transactions = new List<Transaction> { new() { Amount = 999.99m } };

            var total = _service.CalculateTotalAmount(transactions);

            Assert.Equal(999.99m, total);
        }
    }
}
