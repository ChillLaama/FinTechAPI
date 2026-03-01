using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            var users = new List<object>();

            await foreach (var user in pagedEnumerable)
            {
                users.Add(new
                {
                    user.Uid,
                    user.Email,
                    user.DisplayName,
                    user.Disabled
                });
            }

            return Ok(users);
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetUser(string uid)
        {
            try
            {
                var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                return Ok(new
                {
                    user.Uid,
                    user.Email,
                    user.DisplayName,
                    user.Disabled
                });
            }
            catch (FirebaseAuthException)
            {
                return NotFound(new { message = $"User {uid} not found." });
            }
        }

        [HttpDelete("{uid}")]
        public async Task<IActionResult> DeleteUser(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
                return NoContent();
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{uid}/disable")]
        public async Task<IActionResult> DisableUser(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
                {
                    Uid      = uid,
                    Disabled = true
                });
                return NoContent();
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
