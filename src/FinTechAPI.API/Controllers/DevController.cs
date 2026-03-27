using System.Security.Claims;
using FirebaseAdmin.Auth;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using FinTechAPI.API.Filters;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    /// <summary>
    /// Development-only helpers. Not available in Production.
    /// </summary>
    [DevOnly]
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly FirestoreProvider _firestore;
        private readonly IAuthService _authService;

        public DevController(
            FirestoreProvider firestore,
            IAuthService authService)
        {
            _firestore = firestore;
            _authService = authService;
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// Add the specified amount to the account balance.
        /// </summary>
        [Authorize]
        [HttpPost("accounts/{id}/topup")]
        public async Task<IActionResult> TopUp(string id, [FromBody] AmountDto dto)
        {
            if (dto.Amount <= 0) return BadRequest(new { message = "Amount must be positive." });

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var snap = await _firestore.Accounts.Document(id).GetSnapshotAsync();
            if (!snap.Exists) return NotFound(new { message = "Account not found." });

            var doc = snap.ConvertTo<AccountDocument>();
            if (doc.UserId != userId) return Forbid();

            var newBalance = doc.Balance + (double)dto.Amount;
            await _firestore.Accounts.Document(id).UpdateAsync(new Dictionary<string, object>
            {
                ["balance"] = newBalance,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            return Ok(new { accountId = id, newBalance });
        }

        /// <summary>
        /// Set the account balance to an exact value.
        /// </summary>
        [Authorize]
        [HttpPost("accounts/{id}/set-balance")]
        public async Task<IActionResult> SetBalance(string id, [FromBody] AmountDto dto)
        {
            if (dto.Amount < 0) return BadRequest(new { message = "Balance cannot be negative." });

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var snap = await _firestore.Accounts.Document(id).GetSnapshotAsync();
            if (!snap.Exists) return NotFound(new { message = "Account not found." });

            var doc = snap.ConvertTo<AccountDocument>();
            if (doc.UserId != userId) return Forbid();

            await _firestore.Accounts.Document(id).UpdateAsync(new Dictionary<string, object>
            {
                ["balance"] = (double)dto.Amount,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            return Ok(new { accountId = id, newBalance = dto.Amount });
        }

        /// <summary>
        /// Seeds random transactions for the last 30 days.
        /// Defaults to 20 mixed Income/Expense entries with random amounts.
        /// </summary>
        [Authorize]
        [HttpPost("accounts/{id}/seed")]
        public async Task<IActionResult> SeedTransactions(string id, [FromQuery] int count = 20)
        {
            if (count is < 1 or > 200)
                return BadRequest(new { message = "count must be between 1 and 200." });

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var accountSnap = await _firestore.Accounts.Document(id).GetSnapshotAsync();
            if (!accountSnap.Exists) return NotFound(new { message = "Account not found." });

            var accountDoc = accountSnap.ConvertTo<AccountDocument>();
            if (accountDoc.UserId != userId) return Forbid();

            var rng = new Random();
            var categories = new[] { "Food", "Transport", "Salary", "Shopping", "Health", "Entertainment", "Rent", "Utilities" };
            var now = DateTime.UtcNow;
            double balanceDelta = 0;

            var batch = _firestore.Db.StartBatch();

            for (int i = 0; i < count; i++)
            {
                var isIncome = rng.Next(2) == 0;
                long amountMinorUnits = rng.Next(1000, 50000); // 10.00–500.00 in cents
                var txnRef = _firestore.Transactions.Document();

                var txnDoc = new TransactionDocument
                {
                    Id = txnRef.Id,
                    AmountMinorUnits = amountMinorUnits,
                    Currency = accountDoc.Currency,
                    Type = isIncome ? (int)TransactionType.Income : (int)TransactionType.Expense,
                    Description = categories[rng.Next(categories.Length)],
                    TransactionDate = Timestamp.FromDateTime(now.AddDays(-rng.Next(30)).ToUniversalTime()),
                    AccountId = id,
                    UserId = userId,
                    CreatedAt = Timestamp.GetCurrentTimestamp(),
                    UpdatedAt = Timestamp.GetCurrentTimestamp()
                };

                batch.Set(txnRef, txnDoc);
                balanceDelta += isIncome ? amountMinorUnits / 100.0 : -amountMinorUnits / 100.0;
            }

            // Update account balance
            batch.Update(_firestore.Accounts.Document(id), new Dictionary<string, object>
            {
                ["balance"] = accountDoc.Balance + balanceDelta,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            await batch.CommitAsync();

            return Ok(new { created = count, balanceDelta = Math.Round(balanceDelta, 2) });
        }

        /// <summary>
        /// Delete all transactions for the account and reset balance to zero.
        /// </summary>
        [Authorize]
        [HttpDelete("accounts/{id}/transactions")]
        public async Task<IActionResult> ClearTransactions(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var accountSnap = await _firestore.Accounts.Document(id).GetSnapshotAsync();
            if (!accountSnap.Exists) return NotFound(new { message = "Account not found." });

            var accountDoc = accountSnap.ConvertTo<AccountDocument>();
            if (accountDoc.UserId != userId) return Forbid();

            var txnSnap = await _firestore.Transactions
                .WhereEqualTo("accountId", id)
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            // Firestore batch max 500 writes
            const int batchSize = 500;
            var docs = txnSnap.Documents.ToList();
            for (int i = 0; i < docs.Count; i += batchSize)
            {
                var batch = _firestore.Db.StartBatch();
                foreach (var doc in docs.Skip(i).Take(batchSize))
                    batch.Delete(doc.Reference);
                await batch.CommitAsync();
            }

            await _firestore.Accounts.Document(id).UpdateAsync(new Dictionary<string, object>
            {
                ["balance"] = 0.0,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            return Ok(new { deleted = docs.Count, newBalance = 0 });
        }

        /// <summary>
        /// Registers a test user and immediately returns an auth token.
        /// If email is not provided, one is generated automatically.
        [HttpPost("quick-register")]
        public async Task<IActionResult> QuickRegister([FromBody] QuickRegisterDto? dto)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var register = new RegisterUserDto
            {
                Email = dto?.Email ?? $"test_{suffix}@dev.local",
                Password = dto?.Password ?? "Test@1234!",
                FirstName = dto?.FirstName ?? "Test",
                LastName = dto?.LastName ?? "User"
            };

            var (success, error, _) = await _authService.RegisterAsync(register);
            if (!success)
                return BadRequest(new { message = error });

            var auth = await _authService.LoginAsync(new LoginDto
            {
                Email = register.Email,
                Password = register.Password
            });

            return Ok(new
            {
                email = register.Email,
                password = register.Password,
                token = auth.Token,
                auth.Expiration
            });
        }

        /// <summary>
        /// Dev-only: assign a Firebase custom claim role to any user.
        /// POST /api/dev/users/{uid}/role  { "role": "admin" }
        /// User must re-login after this call for the new claim to take effect.
        /// </summary>
        [HttpPost("users/{uid}/role")]
        public async Task<IActionResult> SetUserRoleDev(string uid, [FromBody] SetDevRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return BadRequest(new { message = "uid is required." });

            if (string.IsNullOrWhiteSpace(dto.Role))
                return BadRequest(new { message = "Role is required." });

            var role = dto.Role.Trim().ToLowerInvariant();

            var allowed = new HashSet<string> { "admin", "analyst", "user" };
            if (!allowed.Contains(role))
                return BadRequest(new { message = $"Unsupported role '{role}'. Allowed: {string.Join(", ", allowed)}" });

            try
            {
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                    uid,
                    new Dictionary<string, object> { ["role"] = role });
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new
            {
                uid,
                role,
                message = "Role assigned. User must re-login to get an updated ID token."
            });
        }
    }

    public record AmountDto(decimal Amount);
    public record QuickRegisterDto(string? Email, string? Password, string? FirstName, string? LastName);
    public record SetDevRoleDto(string Role);
}
