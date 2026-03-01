using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    /// <summary>
    /// Manages user roles via Firebase custom claims.
    /// Roles are stored as the "role" custom claim on each Firebase user.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        [HttpGet("{uid}")]
        public async Task<IActionResult> GetUserRole(string uid)
        {
            try
            {
                var user  = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                var claims = user.CustomClaims;
                var role  = claims != null && claims.TryGetValue("role", out var r) ? r?.ToString() : null;
                return Ok(new { uid, role });
            }
            catch (FirebaseAuthException)
            {
                return NotFound(new { message = $"User {uid} not found." });
            }
        }

        [HttpPost("{uid}")]
        public async Task<IActionResult> SetUserRole(string uid, [FromBody] SetRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Role))
                return BadRequest(new { message = "Role is required." });

            try
            {
                var claims = new Dictionary<string, object> { ["role"] = request.Role };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
                return Ok(new { uid, role = request.Role });
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{uid}")]
        public async Task<IActionResult> RemoveUserRole(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, null);
                return NoContent();
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public record SetRoleRequest(string Role);
}
