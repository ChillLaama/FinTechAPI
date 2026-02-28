using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTechAPI.Infrastructure.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly FinTechDbContext _context;

        public SecurityService(FinTechDbContext context)
        {
            _context = context;
        }

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
