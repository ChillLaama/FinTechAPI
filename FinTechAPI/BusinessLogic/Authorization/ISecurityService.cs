using FinTechAPI.Models;

namespace FinTechAPI.BusinessLogic.Authorization;

public interface ISecurityService
{
    IEnumerable<Transaction> DetectAnomalies(decimal threshold);

    Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold);
}