using FinTechAPI.Infrastructure.Firebase;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly FirestoreProvider _firestore;

        public HealthController(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var checks = new Dictionary<string, string>();

            try
            {
                // Verify Firestore is reachable by reading a lightweight collection reference
                await _firestore.Users.Limit(1).GetSnapshotAsync();
                checks["firestore"] = "ok";
            }
            catch
            {
                checks["firestore"] = "unhealthy";
            }

            var allHealthy = checks.Values.All(v => v == "ok");
            var status = allHealthy ? "healthy" : "degraded";

            if (!allHealthy)
                return StatusCode(503, new { status, checks });

            return Ok(new { status, checks });
        }
    }
}
