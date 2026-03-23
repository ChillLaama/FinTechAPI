using System.Security.Claims;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class FraudCasesControllerTests
    {
        private readonly Mock<IFraudCaseService> _mockCaseService;
        private readonly Mock<IFraudService> _mockFraudService;
        private readonly Mock<IAuditService> _mockAudit;
        private readonly FraudCasesController _controller;

        private const string AdminUserId = "admin-user-1";

        public FraudCasesControllerTests()
        {
            _mockCaseService = new Mock<IFraudCaseService>();
            _mockFraudService = new Mock<IFraudService>();
            _mockAudit = new Mock<IAuditService>();
            _controller = new FraudCasesController(_mockCaseService.Object, _mockFraudService.Object, _mockAudit.Object);

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, AdminUserId),
                new Claim(ClaimTypes.Email, "admin@example.com"),
                new Claim(ClaimTypes.Role, "admin")
            }, "mock"));
            context.Items["CorrelationId"] = "corr-123";

            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        // ── GetCases ──────────────────────────────────────────────

        [Fact]
        public async Task GetCases_ShouldReturnOk_WithPagedResults()
        {
            var page = new FraudCasePageDto
            {
                Items = new List<FraudCaseDto>
                {
                    new() { Id = "case-1", Status = "Open", FraudScore = 55 }
                },
                TotalCount = 1
            };

            _mockCaseService.Setup(s => s.GetCasesAsync(null, 20, null)).ReturnsAsync(page);

            var result = await _controller.GetCases();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPage = Assert.IsType<FraudCasePageDto>(okResult.Value);
            Assert.Single(returnedPage.Items);
            Assert.Equal(1, returnedPage.TotalCount);
        }

        [Fact]
        public async Task GetCases_ShouldFilterByStatus()
        {
            var page = new FraudCasePageDto { Items = new(), TotalCount = 0 };
            _mockCaseService.Setup(s => s.GetCasesAsync("Open", 20, null)).ReturnsAsync(page);

            var result = await _controller.GetCases(status: "Open");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        // ── GetCaseById ───────────────────────────────────────────

        [Fact]
        public async Task GetCaseById_ShouldReturnOk_WhenFound()
        {
            var fraudCase = new FraudCaseDto { Id = "case-1", Status = "Open" };
            _mockCaseService.Setup(s => s.GetCaseByIdAsync("case-1")).ReturnsAsync(fraudCase);

            var result = await _controller.GetCaseById("case-1");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("case-1", ((FraudCaseDto)okResult.Value!).Id);
        }

        [Fact]
        public async Task GetCaseById_ShouldReturnNotFound_WhenMissing()
        {
            _mockCaseService.Setup(s => s.GetCaseByIdAsync("missing")).ReturnsAsync((FraudCaseDto?)null);

            var result = await _controller.GetCaseById("missing");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Approve ───────────────────────────────────────────────

        [Fact]
        public async Task ApproveCase_ShouldReturnOk_WhenFound()
        {
            var resolved = new FraudCaseDto { Id = "case-1", Status = "Approved", ResolvedBy = AdminUserId };
            _mockCaseService.Setup(s => s.ApproveCaseAsync("case-1", AdminUserId, null, "corr-123"))
                .ReturnsAsync(resolved);

            var result = await _controller.ApproveCase("case-1");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Approved", ((FraudCaseDto)okResult.Value!).Status);
        }

        [Fact]
        public async Task ApproveCase_ShouldReturnNotFound_WhenMissing()
        {
            _mockCaseService.Setup(s => s.ApproveCaseAsync("missing", AdminUserId, null, "corr-123"))
                .ReturnsAsync((FraudCaseDto?)null);

            var result = await _controller.ApproveCase("missing");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ApproveCase_ShouldPassAnalystNotes()
        {
            var dto = new UpdateFraudCaseDto { AnalystNotes = "Looks legitimate" };
            var resolved = new FraudCaseDto { Id = "case-1", Status = "Approved", AnalystNotes = "Looks legitimate" };
            _mockCaseService.Setup(s => s.ApproveCaseAsync("case-1", AdminUserId, "Looks legitimate", "corr-123"))
                .ReturnsAsync(resolved);

            var result = await _controller.ApproveCase("case-1", dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Looks legitimate", ((FraudCaseDto)okResult.Value!).AnalystNotes);
        }

        // ── Reject ────────────────────────────────────────────────

        [Fact]
        public async Task RejectCase_ShouldReturnOk_WhenFound()
        {
            var resolved = new FraudCaseDto { Id = "case-1", Status = "Rejected", ResolvedBy = AdminUserId };
            _mockCaseService.Setup(s => s.RejectCaseAsync("case-1", AdminUserId, null, "corr-123"))
                .ReturnsAsync(resolved);

            var result = await _controller.RejectCase("case-1");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("Rejected", ((FraudCaseDto)okResult.Value!).Status);
        }

        [Fact]
        public async Task RejectCase_ShouldReturnNotFound_WhenMissing()
        {
            _mockCaseService.Setup(s => s.RejectCaseAsync("missing", AdminUserId, null, "corr-123"))
                .ReturnsAsync((FraudCaseDto?)null);

            var result = await _controller.RejectCase("missing");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Escalate ──────────────────────────────────────────────

        [Fact]
        public async Task EscalateCase_ShouldReturnOk_WhenFound()
        {
            var escalated = new FraudCaseDto { Id = "case-1", Status = "InReview" };
            _mockCaseService.Setup(s => s.EscalateCaseAsync("case-1", null, "corr-123"))
                .ReturnsAsync(escalated);

            var result = await _controller.EscalateCase("case-1");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("InReview", ((FraudCaseDto)okResult.Value!).Status);
        }

        [Fact]
        public async Task EscalateCase_ShouldReturnNotFound_WhenMissing()
        {
            _mockCaseService.Setup(s => s.EscalateCaseAsync("missing", null, "corr-123"))
                .ReturnsAsync((FraudCaseDto?)null);

            var result = await _controller.EscalateCase("missing");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Assign ────────────────────────────────────────────────

        [Fact]
        public async Task AssignCase_ShouldReturnOk_WhenFound()
        {
            var assigned = new FraudCaseDto { Id = "case-1", Assignee = "analyst@example.com", Status = "InReview" };
            _mockCaseService.Setup(s => s.AssignCaseAsync("case-1", "analyst@example.com", "corr-123"))
                .ReturnsAsync(assigned);

            var dto = new AssignFraudCaseDto { Assignee = "analyst@example.com" };
            var result = await _controller.AssignCase("case-1", dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("analyst@example.com", ((FraudCaseDto)okResult.Value!).Assignee);
        }

        [Fact]
        public async Task AssignCase_ShouldReturnNotFound_WhenMissing()
        {
            _mockCaseService.Setup(s => s.AssignCaseAsync("missing", "analyst@example.com", "corr-123"))
                .ReturnsAsync((FraudCaseDto?)null);

            var dto = new AssignFraudCaseDto { Assignee = "analyst@example.com" };
            var result = await _controller.AssignCase("missing", dto);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── GetCaseEvaluation ─────────────────────────────────────

        [Fact]
        public async Task GetCaseEvaluation_ShouldReturnOk_WhenFound()
        {
            var fraudCase = new FraudCaseDto { Id = "case-1", EvaluationId = "eval-1" };
            var evaluation = new FraudEvaluationDto { Id = "eval-1", FraudScore = 60 };

            _mockCaseService.Setup(s => s.GetCaseByIdAsync("case-1")).ReturnsAsync(fraudCase);
            _mockFraudService.Setup(s => s.GetEvaluationByIdAsync("eval-1")).ReturnsAsync(evaluation);

            var result = await _controller.GetCaseEvaluation("case-1");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(60, ((FraudEvaluationDto)okResult.Value!).FraudScore);
        }

        [Fact]
        public async Task GetCaseEvaluation_ShouldReturnNotFound_WhenCaseMissing()
        {
            _mockCaseService.Setup(s => s.GetCaseByIdAsync("missing")).ReturnsAsync((FraudCaseDto?)null);

            var result = await _controller.GetCaseEvaluation("missing");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        // ── Lifecycle: open → assign → approve ────────────────────

        [Fact]
        public async Task Lifecycle_OpenToAssignToApprove()
        {
            // Step 1: Case starts as Open
            var openCase = new FraudCaseDto { Id = "case-1", Status = "Open" };
            _mockCaseService.Setup(s => s.GetCaseByIdAsync("case-1")).ReturnsAsync(openCase);

            // Step 2: Assign
            var assignedCase = new FraudCaseDto { Id = "case-1", Status = "InReview", Assignee = "analyst@example.com" };
            _mockCaseService.Setup(s => s.AssignCaseAsync("case-1", "analyst@example.com", "corr-123"))
                .ReturnsAsync(assignedCase);

            var assignResult = await _controller.AssignCase("case-1", new AssignFraudCaseDto { Assignee = "analyst@example.com" });
            var assignOk = Assert.IsType<OkObjectResult>(assignResult.Result);
            Assert.Equal("InReview", ((FraudCaseDto)assignOk.Value!).Status);

            // Step 3: Approve
            var approvedCase = new FraudCaseDto { Id = "case-1", Status = "Approved", ResolvedBy = AdminUserId };
            _mockCaseService.Setup(s => s.ApproveCaseAsync("case-1", AdminUserId, "Confirmed safe", "corr-123"))
                .ReturnsAsync(approvedCase);

            var approveResult = await _controller.ApproveCase("case-1", new UpdateFraudCaseDto { AnalystNotes = "Confirmed safe" });
            var approveOk = Assert.IsType<OkObjectResult>(approveResult.Result);
            Assert.Equal("Approved", ((FraudCaseDto)approveOk.Value!).Status);
        }
    }
}
