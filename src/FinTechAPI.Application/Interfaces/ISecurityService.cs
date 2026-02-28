using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.Interfaces
{
    public interface ISecurityService
    {
        IEnumerable<Transaction> DetectAnomalies(decimal threshold);
        Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold);
    }
}
