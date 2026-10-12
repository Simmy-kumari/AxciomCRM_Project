using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
[Produces("application/json")]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public LeadsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/leads
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = _context.Leads.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                     l.Email.ToLower().Contains(term) ||
                                     l.LeadCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var leads = await query
            .OrderByDescending(l => l.CreatedDate)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo
            })
            .ToListAsync();

        return Ok(leads);
    }

    // GET /api/leads/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<LeadDto>> GetLead(int id)
    {
        var l = await _context.Leads.FindAsync(id);
        if (l == null) return NotFound(new { message = $"Lead with ID {id} not found." });

        return Ok(new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedTo = l.AssignedTo
        });
    }

    // POST /api/leads
    [HttpPost]
    public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        int nextId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 2000) + 1;

        var lead = new Lead
        {
            LeadCode = $"LEAD-{nextId}",
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = dto.AssignedTo,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId: User.Identity?.Name ?? "API",
            userName: User.Identity?.Name ?? "API User",
            action: "Create",
            entityName: "Lead",
            recordId: lead.LeadCode,
            details: "Created via REST API endpoint.");

        var resultDto = new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            CreatedDate = lead.CreatedDate,
            AssignedTo = lead.AssignedTo
        };

        return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, resultDto);
    }
}
