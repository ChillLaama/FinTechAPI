using System.Security.Claims;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/fraud-cases")]
    [Authorize(Roles = "admin")]
    public class FraudCasesController : ControllerBase
    {
        private readonly IFraudCaseService _caseService;
        private readonly IFraudService _fraudService;
        private readonly IAuditService _audit;

        public FraudCasesController(IFraudCaseService caseService, IFraudService fraudService, IAuditService audit)
        {
            _caseService = caseService;
            _fraudService = fraudService;
            _audit = audit;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        private string? GetCorrelationId() =>
            HttpContext.Items.TryGetValue("CorrelationId", out var val) ? val as string : null;

        [HttpGet]
        public async Task<ActionResult<FraudCasePageDto>> GetCases(
            [FromQuery] string? status = null,
            [FromQuery] int limit = 20,
            [FromQuery] string? startAfter = null)
        {
            if (limit is < 1 or > 100) limit = 20;
            var result = await _caseService.GetCasesAsync(status, limit, startAfter);
            return Ok(result);
        }

        [HttpGet("{caseId}")]
        public async Task<ActionResult<FraudCaseDto>> GetCaseById(string caseId)
        {
            var result = await _caseService.GetCaseByIdAsync(caseId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("{caseId}/evaluation")]
        public async Task<ActionResult<FraudEvaluationDto>> GetCaseEvaluation(string caseId)
        {
            var fraudCase = await _caseService.GetCaseByIdAsync(caseId);
            if (fraudCase == null) return NotFound();

            var evaluation = await _fraudService.GetEvaluationByIdAsync(fraudCase.EvaluationId);
            if (evaluation == null) return NotFound();

            return Ok(evaluation);
        }

        [HttpPost("{caseId}/approve")]
        public async Task<ActionResult<FraudCaseDto>> ApproveCase(string caseId, [FromBody] UpdateFraudCaseDto? dto = null)
        {
            var userId = GetCurrentUserId();
            var correlationId = GetCorrelationId();

            var result = await _caseService.ApproveCaseAsync(caseId, userId, dto?.AnalystNotes, correlationId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost("{caseId}/reject")]
        public async Task<ActionResult<FraudCaseDto>> RejectCase(string caseId, [FromBody] UpdateFraudCaseDto? dto = null)
        {
            var userId = GetCurrentUserId();
            var correlationId = GetCorrelationId();

            var result = await _caseService.RejectCaseAsync(caseId, userId, dto?.AnalystNotes, correlationId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost("{caseId}/escalate")]
        public async Task<ActionResult<FraudCaseDto>> EscalateCase(string caseId, [FromBody] UpdateFraudCaseDto? dto = null)
        {
            var correlationId = GetCorrelationId();

            var result = await _caseService.EscalateCaseAsync(caseId, dto?.AnalystNotes, correlationId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpPost("{caseId}/assign")]
        public async Task<ActionResult<FraudCaseDto>> AssignCase(string caseId, [FromBody] AssignFraudCaseDto dto)
        {
            var correlationId = GetCorrelationId();

            var result = await _caseService.AssignCaseAsync(caseId, dto.Assignee, correlationId);
            if (result == null) return NotFound();

            return Ok(result);
        }
    }
}
