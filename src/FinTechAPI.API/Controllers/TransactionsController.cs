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
        private readonly IMapper             _mapper;

        public TransactionsController(ITransactionService transactionService, IMapper mapper)
        {
            _transactionService = transactionService;
            _mapper             = mapper;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _transactionService.GetTransactionsAsync(userId);
            return Ok(_mapper.Map<IEnumerable<TransactionDto>>(transactions));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transaction = await _transactionService.GetTransactionByIdAsync(id, userId);
            if (transaction == null) return NotFound();

            return Ok(_mapper.Map<TransactionDto>(transaction));
        }

        [HttpGet("account/{accountId}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetByAccount(string accountId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _transactionService.GetTransactionsByAccountIdAsync(accountId, userId);
            return Ok(_mapper.Map<IEnumerable<TransactionDto>>(transactions));
        }

        [HttpPost]
        public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transaction = new Transaction
            {
                Amount          = dto.Amount,
                Currency        = dto.Currency,
                Type            = dto.Type,
                Description     = dto.Description,
                TransactionDate = dto.TransactionDate,
                AccountId       = dto.AccountId
            };

            var created = await _transactionService.CreateTransactionAsync(transaction, userId);
            if (created == null)
                return BadRequest(new { message = "Account not found or access denied." });

            var result = _mapper.Map<TransactionDto>(created);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(string id, [FromBody] CreateTransactionDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactionDetails = new Transaction
            {
                Amount          = dto.Amount,
                Currency        = dto.Currency,
                Type            = dto.Type,
                Description     = dto.Description,
                TransactionDate = dto.TransactionDate,
                AccountId       = dto.AccountId
            };

            var updated = await _transactionService.UpdateTransactionAsync(id, transactionDetails, userId);
            if (updated == null) return NotFound();

            return NoContent();
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
    }
}
