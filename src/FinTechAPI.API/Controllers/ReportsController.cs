using System.Security.Claims;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportsController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        private string GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User ID not found in token.");
            return userId;
        }

        [HttpGet("by-type/{transactionType}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsByTypeReport(TransactionType transactionType)
        {
            var userId = GetCurrentUserId();
            var userTransactions = await _reportingService.GetTransactionsByTypeAsync(transactionType, userId);

            var transactionDtos = userTransactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Currency = t.Currency,
                Type = t.Type,
                Description = t.Description,
                TransactionDate = t.TransactionDate,
                CreatedAt = t.CreatedAt,
                AccountId = t.AccountId
            });

            return Ok(transactionDtos);
        }
    }
}
