using System.Security.Claims;
using AutoMapper;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;

        public TransactionsController(ITransactionService transactionService, IPaymentService paymentService, IMapper mapper)
        {
            _transactionService = transactionService;
            _paymentService = paymentService;
            _mapper = mapper;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _transactionService.GetTransactionsAsync(userId);
            var result = _mapper.Map<IEnumerable<TransactionDto>>(transactions).ToList();
            await EnrichProviderStatusesAsync(result, userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);
            if (transaction == null) return NotFound();

            var dto = _mapper.Map<TransactionDto>(transaction);
            await EnrichProviderStatusAsync(dto, userId);
            return Ok(dto);
        }

        [HttpGet("account/{accountId}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetByAccount(string accountId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId);
            var result = _mapper.Map<IEnumerable<TransactionDto>>(transactions).ToList();
            await EnrichProviderStatusesAsync(result, userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transaction = new Transaction
            {
                Amount = dto.Amount,
                Currency = dto.Currency,
                Type = dto.Type,
                Status = dto.Status,
                Category = dto.Category,
                Description = dto.Description,
                TransactionDate = dto.TransactionDate,
                AccountId = dto.AccountId
            };

            var created = await _transactionService.CreateTransactionAsync(transaction, userId);
            if (created == null)
                return BadRequest(new { message = "Account not found or access denied." });

            var result = _mapper.Map<TransactionDto>(created);
            await EnrichProviderStatusAsync(result, userId);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(string id, [FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactionDetails = new Transaction
            {
                Amount = dto.Amount,
                Currency = dto.Currency,
                Type = dto.Type,
                Status = dto.Status,
                Category = dto.Category,
                Description = dto.Description,
                TransactionDate = dto.TransactionDate,
                AccountId = dto.AccountId
            };

            var updated = await _transactionService.UpdateTransactionAsync(id, transactionDetails, userId);
            if (updated == null) return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<TransactionDto>> UpdateTransactionStatus(string id, [FromBody] UpdateTransactionStatusDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var updated = await _transactionService.UpdateTransactionStatusAsync(id, dto.Status, userId);
            if (updated == null) return NotFound();

            var result = _mapper.Map<TransactionDto>(updated);
            await EnrichProviderStatusAsync(result, userId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _transactionService.DeleteTransactionAsync(id, userId);
            if (!success) return NotFound();

            return NoContent();
        }

        private async Task EnrichProviderStatusesAsync(ICollection<TransactionDto> transactions, string userId)
        {
            var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
            var latestPaymentByTransactionId = payments
                .Where(payment => !string.IsNullOrWhiteSpace(payment.TransactionId))
                .GroupBy(payment => payment.TransactionId!, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(payment => payment.UpdatedAt)
                        .FirstOrDefault(),
                    StringComparer.Ordinal);

            foreach (var transaction in transactions)
            {
                transaction.BusinessStatus = transaction.Status;

                var latestPayment = latestPaymentByTransactionId.GetValueOrDefault(transaction.Id);
                transaction.ProviderStatus = latestPayment?.Status;
                transaction.ProviderReference = latestPayment?.StripePaymentIntentId;
                transaction.PaymentId = latestPayment?.Id;
                transaction.WebhookEvent = latestPayment?.LastWebhookEvent;
                transaction.CorrelationId = latestPayment?.LastStripeEventId;
                transaction.ProviderUpdatedAt = latestPayment?.UpdatedAt;
            }
        }

        private async Task EnrichProviderStatusAsync(TransactionDto transaction, string userId)
        {
            var list = new List<TransactionDto> { transaction };
            await EnrichProviderStatusesAsync(list, userId);
        }
    }
}
