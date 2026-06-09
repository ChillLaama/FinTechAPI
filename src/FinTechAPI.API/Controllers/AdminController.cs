using System.Security.Claims;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    /// <summary>
    /// Admin-only operations: audit trail, system alerts, reconciliation status.
    /// All endpoints require the "admin" role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAuditService _audit;
        private readonly ISystemAlertService _systemAlerts;
        private readonly IPaymentService _paymentService;
        private readonly IFraudCaseService _fraudCaseService;

        public AdminController(
            IAuditService audit,
            ISystemAlertService systemAlerts,
            IPaymentService paymentService,
            IFraudCaseService fraudCaseService)
        {
            _audit = audit;
            _systemAlerts = systemAlerts;
            _paymentService = paymentService;
            _fraudCaseService = fraudCaseService;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // ── Audit Trail ───────────────────────────────────────────────────────

        /// <summary>
        /// Query audit log entries with optional filters.
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs(
            [FromQuery] string? userId = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int limit = 50)
        {
            var query = new AuditLogQueryDto
            {
                UserId = userId,
                EntityType = entityType,
                Action = action,
                From = from,
                To = to,
                Limit = Math.Clamp(limit, 1, 200)
            };

            var logs = await _audit.QueryAsync(query);
            return Ok(logs);
        }

        // ── System Alerts ─────────────────────────────────────────────────────

        /// <summary>
        /// Get active (non-dismissed) system alerts.
        /// </summary>
        [HttpGet("alerts")]
        public async Task<ActionResult<IReadOnlyList<SystemAlertDto>>> GetAlerts([FromQuery] int limit = 50)
        {
            var alerts = await _systemAlerts.GetActiveAlertsAsync(Math.Clamp(limit, 1, 200));
            return Ok(alerts);
        }

        /// <summary>
        /// Dismiss a system alert by ID.
        /// </summary>
        [HttpPost("alerts/{alertId}/dismiss")]
        public async Task<ActionResult> DismissAlert(string alertId)
        {
            await _systemAlerts.DismissAsync(alertId);
            await _audit.LogAsync(GetCurrentUserId(), "SystemAlert.Dismissed", "SystemAlert", alertId);
            return NoContent();
        }

        // ── Reconciliation Center ─────────────────────────────────────────────

        /// <summary>
        /// Summary of payments that may need reconciliation.
        /// </summary>
        [HttpGet("reconciliation/summary")]
        public async Task<ActionResult<ReconciliationSummaryDto>> GetReconciliationSummary(
            [FromQuery] int staleAfterMinutes = 5)
        {
            var pending = await _paymentService.GetPendingPaymentsForAdminAsync(staleAfterMinutes);

            var summary = new ReconciliationSummaryDto
            {
                PendingPaymentsCount = pending.Count,
                StuckPaymentsCount = pending.Count(p => p.StaleMinutes > 30),
                TotalPaymentsCount = pending.Count,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(summary);
        }

        /// <summary>
        /// List of payments currently stuck in a non-terminal state.
        /// </summary>
        [HttpGet("reconciliation/pending")]
        public async Task<ActionResult<IReadOnlyList<PendingPaymentDto>>> GetPendingPayments(
            [FromQuery] int staleAfterMinutes = 5,
            [FromQuery] int limit = 100)
        {
            var pending = await _paymentService.GetPendingPaymentsForAdminAsync(
                staleAfterMinutes, Math.Clamp(limit, 1, 200));
            return Ok(pending);
        }

        // ── Admin Overview ────────────────────────────────────────────────────

        /// <summary>
        /// Quick stats for the admin panel overview.
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<AdminOverviewDto>> GetOverview()
        {
            var alertsTask = _systemAlerts.GetActiveAlertsAsync(100);
            var pendingTask = _paymentService.GetPendingPaymentsForAdminAsync(5, 100);
            var openCasesTask = _fraudCaseService.GetCasesAsync("Open", 100);

            await Task.WhenAll(alertsTask, pendingTask, openCasesTask);

            var alerts = await alertsTask;
            var pending = await pendingTask;
            var openCases = await openCasesTask;

            return Ok(new AdminOverviewDto
            {
                ActiveAlertsCount = alerts.Count,
                CriticalAlertsCount = alerts.Count(a => a.Severity == "critical"),
                PendingPaymentsCount = pending.Count,
                StuckPaymentsCount = pending.Count(p => p.StaleMinutes > 30),
                OpenFraudCasesCount = openCases.TotalCount,
                GeneratedAt = DateTime.UtcNow
            });
        }
    }

}



