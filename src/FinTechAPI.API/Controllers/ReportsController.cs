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
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly IMapper           _mapper;

        public ReportsController(IReportingService reportingService, IMapper mapper)
        {
            _reportingService = reportingService;
            _mapper           = mapper;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("User ID not found in token.");

        [HttpGet("by-type/{transactionType}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsByTypeReport(TransactionType transactionType)
        {
            var userId       = GetCurrentUserId();
            var transactions = await _reportingService.GetTransactionsByTypeAsync(transactionType, userId);
            return Ok(_mapper.Map<IEnumerable<TransactionDto>>(transactions));
        }
    }
}
