using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /api/reports/pipeline
    [HttpGet("pipeline")]
    public async Task<ActionResult<PipelineReportDto>> GetPipelineReport()
    {
        var opps = await _context.Opportunities
            .Include(o => o.Customer)
            .Where(o => o.Status == "Open")
            .ToListAsync();

        var stages = new[] { "Qualification", "Proposal", "Negotiation" };
        var stageBreakdown = stages.ToDictionary(
            s => s, 
            s => opps.Where(o => o.Stage == s).Sum(o => o.Amount)
        );

        var topDeals = opps
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo,
                CreatedDate = o.CreatedDate
            })
            .ToList();

        var dto = new PipelineReportDto
        {
            TotalPipelineValue = opps.Sum(o => o.Amount),
            WeightedPipelineValue = opps.Sum(o => (o.Amount * o.Probability) / 100m),
            TotalOpenDeals = opps.Count,
            StageBreakdown = stageBreakdown,
            TopDeals = topDeals
        };

        return Ok(dto);
    }
}
