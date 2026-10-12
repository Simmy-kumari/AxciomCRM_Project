using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
[Produces("application/json")]
public class FollowUpsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/followups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FollowUpDto>>> GetFollowUps([FromQuery] string? status)
    {
        var query = _context.FollowUps.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        var list = await query
            .OrderBy(f => f.FollowUpDate)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                RelatedType = f.RelatedType,
                RelatedId = f.RelatedId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            })
            .ToListAsync();

        return Ok(list);
    }

    // POST /api/followups
    [HttpPost]
    public async Task<ActionResult<FollowUpDto>> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
        }

        var followUp = new FollowUp
        {
            RelatedType = dto.RelatedType,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject.Trim(),
            Remarks = dto.Remarks,
            Status = dto.Status,
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId: User.Identity?.Name ?? "API",
            userName: User.Identity?.Name ?? "API User",
            action: "Create",
            entityName: "FollowUp",
            recordId: followUp.FollowUpId.ToString(),
            details: "Created via REST API endpoint.");

        return Ok(new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            RelatedType = followUp.RelatedType,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo
        });
    }
}
