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
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;

        public AccountsController(IAccountService accountService, IMapper mapper)
        {
            _accountService = accountService;
            _mapper = mapper;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            var accounts = await _accountService.GetAccountsByUserIdAsync(userId);
            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountDto>> GetAccount(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            var account = await _accountService.GetAccountByIdAsync(id, userId);
            if (account == null)
                return NotFound(new { message = $"Account with ID {id} not found for the current user." });

            var accountDto = _mapper.Map<AccountDto>(account);
            return Ok(accountDto);
        }

        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            var createdAccount = await _accountService.CreateAccountAsync(account, userId);
            return CreatedAtAction(nameof(GetAccount), new { id = createdAccount.Id }, createdAccount);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] Account accountUpdateDetails)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            if (id != accountUpdateDetails.Id)
                return BadRequest(new { message = "Account ID in URL and body do not match." });

            var updatedAccount = await _accountService.UpdateAccountAsync(id, accountUpdateDetails, userId);
            if (updatedAccount == null)
            {
                if (!await _accountService.AccountExistsAsync(id, userId))
                    return NotFound(new { message = $"Account with ID {id} not found for the current user." });
                return NotFound(new { message = $"Account with ID {id} not found or update failed." });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token." });

            var success = await _accountService.DeleteAccountAsync(id, userId);
            if (!success)
                return NotFound(new { message = $"Account with ID {id} not found for the current user." });

            return NoContent();
        }
    }
}
