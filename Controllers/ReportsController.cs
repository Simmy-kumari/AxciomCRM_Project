using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string type = "pipeline", DateTime? startDate = null, DateTime? endDate = null, string? selectedUser = null)
    {
        var model = new ReportDashboardViewModel
        {
            ActiveReport = type.ToLower(),
            StartDate = startDate,
            EndDate = endDate,
            SelectedUser = selectedUser
        };

        // Queries
        var custQ = _context.Customers.AsQueryable();
        var leadQ = _context.Leads.AsQueryable();
        var oppQ = _context.Opportunities.Include(o => o.Customer).AsQueryable();
        var followQ = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsQueryable();
        var actQ = _context.Activities.Include(a => a.Customer).Include(a => a.Lead).AsQueryable();
        var auditQ = _context.AuditLogs.AsQueryable();

        // Role restriction: SalesExecutive sees only assigned records
        if (User.IsInRole("SalesExecutive"))
        {
            var currentUser = await _userManager.GetUserAsync(User);
            string uid = currentUser?.Id ?? "";
            custQ = custQ.Where(c => c.OwnerId == uid);
            leadQ = leadQ.Where(l => l.AssignedTo == uid);
            oppQ = oppQ.Where(o => o.AssignedTo == uid);
            followQ = followQ.Where(f => f.AssignedTo == uid);
            actQ = actQ.Where(a => a.AssignedTo == uid);
            auditQ = auditQ.Where(a => a.UserId == uid);
        }
        else if (!string.IsNullOrEmpty(selectedUser))
        {
            custQ = custQ.Where(c => c.OwnerId == selectedUser);
            leadQ = leadQ.Where(l => l.AssignedTo == selectedUser);
            oppQ = oppQ.Where(o => o.AssignedTo == selectedUser);
            followQ = followQ.Where(f => f.AssignedTo == selectedUser);
            actQ = actQ.Where(a => a.AssignedTo == selectedUser);
        }

        if (startDate.HasValue)
        {
            custQ = custQ.Where(c => c.CreatedDate >= startDate.Value);
            leadQ = leadQ.Where(l => l.CreatedDate >= startDate.Value);
            oppQ = oppQ.Where(o => o.CreatedDate >= startDate.Value);
            followQ = followQ.Where(f => f.FollowUpDate >= startDate.Value);
            actQ = actQ.Where(a => a.ActivityDate >= startDate.Value);
            auditQ = auditQ.Where(a => a.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1);
            custQ = custQ.Where(c => c.CreatedDate < endOfDay);
            leadQ = leadQ.Where(l => l.CreatedDate < endOfDay);
            oppQ = oppQ.Where(o => o.CreatedDate < endOfDay);
            followQ = followQ.Where(f => f.FollowUpDate < endOfDay);
            actQ = actQ.Where(a => a.ActivityDate < endOfDay);
            auditQ = auditQ.Where(a => a.CreatedDate < endOfDay);
        }

        var allOpps = await oppQ.ToListAsync();
        var allLeads = await leadQ.ToListAsync();
        var allCusts = await custQ.ToListAsync();
        var allFollows = await followQ.ToListAsync();
        var allActs = await actQ.ToListAsync();
        var allAudits = await auditQ.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();

        // Summary calculations
        model.TotalCustomersCount = allCusts.Count;
        model.TotalLeadsCount = allLeads.Count;
        int convertedLeads = allLeads.Count(l => l.Status == "Converted");
        model.LeadConversionRate = allLeads.Count > 0 ? Math.Round(((double)convertedLeads / allLeads.Count) * 100, 1) : 0;

        model.TotalPipelineValue = allOpps.Where(o => o.Status == "Open").Sum(o => o.Amount);
        model.TotalWeightedPipeline = allOpps.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m);
        model.TotalRevenueWon = allOpps.Where(o => o.Stage == "Won" || o.Status == "Won").Sum(o => o.Amount);
        model.CompletedFollowUpsCount = allFollows.Count(f => f.Status == "Completed");
        model.TotalActivitiesCount = allActs.Count;

        // Chart 1: Pipeline Stage Breakdown
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        model.PipelineStageNames = stages.ToList();
        model.PipelineStageAmounts = stages.Select(s => allOpps.Where(o => o.Stage == s).Sum(o => o.Amount)).ToList();

        // Chart 2: Lead Sources Breakdown
        var sourceGroups = allLeads.GroupBy(l => l.Source).OrderByDescending(g => g.Count()).Take(5).ToList();
        model.LeadSources = sourceGroups.Select(g => g.Key).ToList();
        model.LeadSourceCounts = sourceGroups.Select(g => g.Count()).ToList();

        // Chart 3: Activity Breakdown
        var actTypes = new[] { "Call", "Meeting", "Email", "Task" };
        model.ActivityTypeNames = actTypes.ToList();
        model.ActivityTypeCounts = actTypes.Select(t => allActs.Count(a => a.ActivityType == t)).ToList();

        // Chart 4: Owner-wise Pipeline breakdown
        var users = await _userManager.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        var ownerGroup = allOpps.Where(o => o.Status == "Open" && o.AssignedTo != null)
            .GroupBy(o => o.AssignedTo!)
            .ToList();
        model.OwnerNames = ownerGroup.Select(g => users.ContainsKey(g.Key) ? users[g.Key] : "Other").ToList();
        model.OwnerPipelineAmounts = ownerGroup.Select(g => g.Sum(o => o.Amount)).ToList();

        // Tables
        model.CustomersData = allCusts;
        model.LeadsData = allLeads;
        model.OpportunitiesData = allOpps;
        model.FollowUpsData = allFollows;
        model.ActivitiesData = allActs;
        model.AuditLogsData = allAudits;

        ViewBag.TeamUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();

        return View(model);
    }
}
