using FinTechAPI.Data;
using FinTechAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.BusinessLogic.Authorization
{
    public class SecurityService : ISecurityService
    {
        private readonly FinTechDbContext _context;

        public SecurityService(FinTechDbContext context)
        {
            _context = context;
        }

        // Simple anomaly detection based on transaction amount threshold
        public IEnumerable<Transaction> DetectAnomalies(decimal threshold)
        {
            return _context.Transactions.Where(t => t.Amount > threshold).ToList();
        }
        
        public async Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold)
        {
            return await _context.Transactions
                .Where(t => t.Amount > threshold)
                .ToListAsync();
        }

    }
}