using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
[Produces("application/json")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunitiesApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/opportunities
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? stage, [FromQuery] string? status)
    {
        var query = _context.Opportunities.Include(o => o.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var list = await query
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo,
                CreatedDate = o.CreatedDate
            })
            .ToListAsync();

        return Ok(list);
    }

    // POST /api/opportunities
    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
        }

        if (dto.Status == "Open" && dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Expected Close Date cannot be in the past for active opportunities." });
        }

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            AssignedTo = dto.AssignedTo,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId: User.Identity?.Name ?? "API",
            userName: User.Identity?.Name ?? "API User",
            action: "Create",
            entityName: "Opportunity",
            recordId: opp.OpportunityId.ToString(),
            details: "Created via REST API endpoint.");

        var res = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            CreatedDate = opp.CreatedDate
        };

        return Ok(res);
    }
}
