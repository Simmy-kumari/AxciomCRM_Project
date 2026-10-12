using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Users
        var usersToSeed = new[]
        {
            new { Email = "admin@acxiom.com", FullName = "Alexander Wright", Role = "Admin", Dept = "Executive & IT", Password = "Admin@123!" },
            new { Email = "manager@acxiom.com", FullName = "Sarah Jenkins", Role = "Manager", Dept = "Sales Operations", Password = "Manager@123!" },
            new { Email = "sales@acxiom.com", FullName = "David Miller", Role = "SalesExecutive", Dept = "Enterprise Sales", Password = "Sales@123!" },
            new { Email = "emma@acxiom.com", FullName = "Emma Watson", Role = "SalesExecutive", Dept = "Mid-Market Sales", Password = "Sales@123!" }
        };

        foreach (var seed in usersToSeed)
        {
            var existing = await userManager.FindByEmailAsync(seed.Email);
            if (existing == null)
            {
                var newUser = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    FullName = seed.FullName,
                    Department = seed.Dept,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };

                var result = await userManager.CreateAsync(newUser, seed.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, seed.Role);
                }
            }
        }

        // 3. Seed Customers
        if (!context.Customers.Any())
        {
            var adminUser = await userManager.FindByEmailAsync("admin@acxiom.com");
            var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
            var emmaUser = await userManager.FindByEmailAsync("emma@acxiom.com");

            var customers = new List<Customer>
            {
                new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "John Carter",
                    CompanyName = "Apex Global Dynamics",
                    Email = "john.carter@apexglobal.com",
                    Phone = "+1 555-019-2834",
                    Address = "742 Evergreen Terrace, Suite 400",
                    City = "New York",
                    State = "NY",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-4),
                    CreatedBy = adminUser?.Id,
                    OwnerId = salesUser?.Id,
                    Notes = "Strategic enterprise client. Interested in multi-year ERP integration."
                },
                new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Maya Lin",
                    CompanyName = "BlueWave Infotech",
                    Email = "maya.lin@bluewaveinfo.com",
                    Phone = "+1 555-014-9921",
                    Address = "1200 Tech Ridge Parkway",
                    City = "Austin",
                    State = "TX",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-3),
                    CreatedBy = adminUser?.Id,
                    OwnerId = salesUser?.Id,
                    Notes = "SaaS client with 250 active licenses. Contract renews Q4."
                },
                new Customer
                {
                    CustomerCode = "CUST-1003",
                    CustomerName = "Robert Thorne",
                    CompanyName = "Crestview Financial",
                    Email = "rthorne@crestviewfin.com",
                    Phone = "+1 555-018-3482",
                    Address = "333 Wacker Drive, Floor 18",
                    City = "Chicago",
                    State = "IL",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy = adminUser?.Id,
                    OwnerId = emmaUser?.Id,
                    Notes = "Strict compliance, SOC-2 requirements needed."
                },
                new Customer
                {
                    CustomerCode = "CUST-1004",
                    CustomerName = "Dr. Elena Rostova",
                    CompanyName = "Horizon Health Logistics",
                    Email = "elena.rostova@horizonhl.com",
                    Phone = "+1 555-016-7241",
                    Address = "450 Medical Center Blvd",
                    City = "Boston",
                    State = "MA",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-5),
                    CreatedBy = adminUser?.Id,
                    OwnerId = emmaUser?.Id,
                    Notes = "HIPAA-ready cloud platform integration."
                },
                new Customer
                {
                    CustomerCode = "CUST-1005",
                    CustomerName = "Carlos Gomez",
                    CompanyName = "Vertex Industrial Robotics",
                    Email = "carlos.g@vertexrobotics.io",
                    Phone = "+1 555-017-8829",
                    Address = "88 Foundry Street",
                    City = "San Francisco",
                    State = "CA",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-1),
                    CreatedBy = adminUser?.Id,
                    OwnerId = salesUser?.Id,
                    Notes = "High growth industrial robotics manufacturer."
                },
                new Customer
                {
                    CustomerCode = "CUST-1006",
                    CustomerName = "Sophie Chen",
                    CompanyName = "Pinnacle Retail Solutions",
                    Email = "sophie.chen@pinnacleretail.com",
                    Phone = "+1 555-013-4412",
                    Address = "1500 4th Ave, Suite 900",
                    City = "Seattle",
                    State = "WA",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6),
                    CreatedBy = adminUser?.Id,
                    OwnerId = emmaUser?.Id,
                    Notes = "Expanding across 40 new brick-and-mortar storefronts."
                },
                new Customer
                {
                    CustomerCode = "CUST-1007",
                    CustomerName = "Liam Anderson",
                    CompanyName = "Quantum Cloud Systems",
                    Email = "liam.a@quantumcloud.net",
                    Phone = "+1 555-012-7744",
                    Address = "1700 Broadway, Suite 300",
                    City = "Denver",
                    State = "CO",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy = adminUser?.Id,
                    OwnerId = salesUser?.Id,
                    Notes = "Hybrid-cloud infrastructure integration."
                },
                new Customer
                {
                    CustomerCode = "CUST-1008",
                    CustomerName = "Priya Patel",
                    CompanyName = "Starlight Media Works",
                    Email = "priya.patel@starlightmedia.com",
                    Phone = "+1 555-011-3329",
                    Address = "9000 Sunset Blvd",
                    City = "Los Angeles",
                    State = "CA",
                    Status = "Inactive",
                    CreatedDate = DateTime.UtcNow.AddMonths(-7),
                    CreatedBy = adminUser?.Id,
                    OwnerId = emmaUser?.Id,
                    Notes = "Account temporarily paused pending seasonal restructuring."
                }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        // 4. Seed Leads
        if (!context.Leads.Any())
        {
            var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
            var emmaUser = await userManager.FindByEmailAsync("emma@acxiom.com");

            var leads = new List<Lead>
            {
                new Lead
                {
                    LeadCode = "LEAD-2001",
                    LeadName = "Mark Henderson",
                    CompanyName = "Nova Energy Grid",
                    Email = "mark.h@novaenergy.com",
                    Phone = "+1 555-021-3490",
                    Source = "Website",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 75000,
                    CreatedDate = DateTime.UtcNow.AddDays(-12),
                    AssignedTo = salesUser?.Id,
                    Notes = "Submitted contact form requesting demo for smart grid dashboard."
                },
                new Lead
                {
                    LeadCode = "LEAD-2002",
                    LeadName = "Rachel Green",
                    CompanyName = "CyberShield Security",
                    Email = "rachel.g@cybershield.io",
                    Phone = "+1 555-022-7711",
                    Source = "Referral",
                    Status = "Contacted",
                    Priority = "Medium",
                    ExpectedValue = 45000,
                    CreatedDate = DateTime.UtcNow.AddDays(-8),
                    AssignedTo = salesUser?.Id,
                    Notes = "Introduced by John Carter from Apex Global."
                },
                new Lead
                {
                    LeadCode = "LEAD-2003",
                    LeadName = "Ethan Cole",
                    CompanyName = "AgriTech Precision",
                    Email = "ethan.cole@agritechprecision.com",
                    Phone = "+1 555-023-9988",
                    Source = "Trade Show",
                    Status = "New",
                    Priority = "High",
                    ExpectedValue = 120000,
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    AssignedTo = emmaUser?.Id,
                    Notes = "Met at Chicago IoT Expo. Needs fleet telemetry solution."
                },
                new Lead
                {
                    LeadCode = "LEAD-2004",
                    LeadName = "Zoe Vance",
                    CompanyName = "Beacon Digital Media",
                    Email = "zoe.vance@beacondigital.org",
                    Phone = "+1 555-024-5544",
                    Source = "Email Campaign",
                    Status = "Contacted",
                    Priority = "Low",
                    ExpectedValue = 28000,
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    AssignedTo = emmaUser?.Id,
                    Notes = "Clicked through Q3 product newsletter campaign."
                },
                new Lead
                {
                    LeadCode = "LEAD-2005",
                    LeadName = "Daniel Brooks",
                    CompanyName = "OmniLogistics Network",
                    Email = "dbrooks@omnilogistics.com",
                    Phone = "+1 555-025-1123",
                    Source = "Website",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 95000,
                    CreatedDate = DateTime.UtcNow.AddDays(-6),
                    AssignedTo = salesUser?.Id,
                    Notes = "Ready for technical validation meeting."
                },
                new Lead
                {
                    LeadCode = "LEAD-2006",
                    LeadName = "Jessica Wu",
                    CompanyName = "Zenith Biotech Labs",
                    Email = "jessica.wu@zenithbiotech.com",
                    Phone = "+1 555-026-6632",
                    Source = "Partner",
                    Status = "Unqualified",
                    Priority = "Low",
                    ExpectedValue = 15000,
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    AssignedTo = emmaUser?.Id,
                    Notes = "Budget unapproved for current fiscal year."
                },
                new Lead
                {
                    LeadCode = "LEAD-2007",
                    LeadName = "Thomas King",
                    CompanyName = "IronClad Defense Systems",
                    Email = "tking@ironcladdef.com",
                    Phone = "+1 555-027-4481",
                    Source = "Social Media",
                    Status = "Lost",
                    Priority = "Medium",
                    ExpectedValue = 60000,
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    AssignedTo = salesUser?.Id,
                    Notes = "Competitor selected due to pre-existing on-prem hardware."
                },
                new Lead
                {
                    LeadCode = "LEAD-2008",
                    LeadName = "Clara Oswald",
                    CompanyName = "Pulse Health Analytics",
                    Email = "clara.o@pulsehealth.ai",
                    Phone = "+1 555-028-2299",
                    Source = "Referral",
                    Status = "Converted",
                    Priority = "High",
                    ExpectedValue = 88000,
                    CreatedDate = DateTime.UtcNow.AddDays(-40),
                    AssignedTo = emmaUser?.Id,
                    Notes = "Converted to active pipeline opportunity."
                }
            };

            await context.Leads.AddRangeAsync(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities
        if (!context.Opportunities.Any())
        {
            var custApex = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var custBlue = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1002");
            var custCrest = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1003");
            var custVertex = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1005");
            var custPinnacle = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1006");
            var custQuantum = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1007");
            var custHorizon = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1004");
            var custStarlight = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1008");

            var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
            var emmaUser = await userManager.FindByEmailAsync("emma@acxiom.com");

            var opportunities = new List<Opportunity>
            {
                new Opportunity
                {
                    OpportunityName = "Enterprise Cloud Migration",
                    CustomerId = custApex?.CustomerId,
                    Amount = 140000,
                    Stage = "Negotiation",
                    Probability = 75,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(20),
                    Status = "Open",
                    Source = "Customer Expansion",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-25),
                    Notes = "Final commercial terms review underway with procurement."
                },
                new Opportunity
                {
                    OpportunityName = "Financial Analytics Modernization",
                    CustomerId = custCrest?.CustomerId,
                    Amount = 95000,
                    Stage = "Proposal",
                    Probability = 60,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(35),
                    Status = "Open",
                    Source = "Referral",
                    AssignedTo = emmaUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-18),
                    Notes = "Submitted RFP response with executive summary."
                },
                new Opportunity
                {
                    OpportunityName = "Warehouse Automation Suite",
                    CustomerId = custVertex?.CustomerId,
                    Amount = 210000,
                    Stage = "Qualification",
                    Probability = 30,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                    Status = "Open",
                    Source = "Website",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    Notes = "Technical assessment scheduled with engineering VP."
                },
                new Opportunity
                {
                    OpportunityName = "Multi-Cloud Disaster Recovery",
                    CustomerId = custQuantum?.CustomerId,
                    Amount = 180000,
                    Stage = "Proposal",
                    Probability = 50,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                    Status = "Open",
                    Source = "Partner",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-14),
                    Notes = "Evaluating active-active replication architecture."
                },
                new Opportunity
                {
                    OpportunityName = "Creative Asset Portal",
                    CustomerId = custStarlight?.CustomerId,
                    Amount = 85000,
                    Stage = "Negotiation",
                    Probability = 80,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(10),
                    Status = "Open",
                    Source = "Referral",
                    AssignedTo = emmaUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-28),
                    Notes = "Contract revision 2.1 undergoing legal approval."
                },
                new Opportunity
                {
                    OpportunityName = "Annual Support & SLA Renewal",
                    CustomerId = custBlue?.CustomerId,
                    Amount = 65000,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Won",
                    Source = "Account Management",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-60),
                    Notes = "Closed deal. 2-year service renewal executed."
                },
                new Opportunity
                {
                    OpportunityName = "Omnichannel E-commerce Integration",
                    CustomerId = custPinnacle?.CustomerId,
                    Amount = 125000,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(-12),
                    Status = "Won",
                    Source = "Direct Outreach",
                    AssignedTo = emmaUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-70),
                    Notes = "Won contract against competing proposal."
                },
                new Opportunity
                {
                    OpportunityName = "Legacy Hospital CRM Migration",
                    CustomerId = custHorizon?.CustomerId,
                    Amount = 48000,
                    Stage = "Lost",
                    Probability = 0,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(-20),
                    Status = "Lost",
                    Source = "Cold Outreach",
                    AssignedTo = emmaUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-80),
                    Notes = "Client delayed migration to next calendar year."
                }
            };

            await context.Opportunities.AddRangeAsync(opportunities);
            await context.SaveChangesAsync();
        }

        // 6. Seed FollowUps
        if (!context.FollowUps.Any())
        {
            var custApex = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var custBlue = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1002");
            var leadNova = context.Leads.FirstOrDefault(l => l.LeadCode == "LEAD-2001");
            var oppApex = context.Opportunities.FirstOrDefault(o => o.OpportunityName == "Enterprise Cloud Migration");

            var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
            var emmaUser = await userManager.FindByEmailAsync("emma@acxiom.com");

            var followUps = new List<FollowUp>
            {
                new FollowUp
                {
                    RelatedType = "Opportunity",
                    RelatedId = oppApex?.OpportunityId ?? 1,
                    OpportunityId = oppApex?.OpportunityId,
                    FollowUpDate = DateTime.UtcNow.AddDays(1).AddHours(3),
                    FollowUpType = "Meeting",
                    Subject = "Executive Commercial Term Alignment",
                    Remarks = "Walk through final service scope and pricing matrix with procurement team.",
                    Status = "Planned",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-2)
                },
                new FollowUp
                {
                    RelatedType = "Lead",
                    RelatedId = leadNova?.LeadId ?? 1,
                    LeadId = leadNova?.LeadId,
                    FollowUpDate = DateTime.UtcNow.AddDays(2).AddHours(5),
                    FollowUpType = "Demo",
                    Subject = "Smart Grid Software Product Walkthrough",
                    Remarks = "Demonstrate live telemetry stream and alerting features.",
                    Status = "Planned",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-1)
                },
                new FollowUp
                {
                    RelatedType = "Customer",
                    RelatedId = custApex?.CustomerId ?? 1,
                    CustomerId = custApex?.CustomerId,
                    FollowUpDate = DateTime.UtcNow.AddDays(3).AddHours(2),
                    FollowUpType = "Call",
                    Subject = "Quarterly Partnership Health Check",
                    Remarks = "Check customer satisfaction and upcoming Q4 cloud roadmap.",
                    Status = "Planned",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                },
                new FollowUp
                {
                    RelatedType = "Customer",
                    RelatedId = custBlue?.CustomerId ?? 2,
                    CustomerId = custBlue?.CustomerId,
                    FollowUpDate = DateTime.UtcNow.AddDays(-1),
                    FollowUpType = "Call",
                    Subject = "Onboarding Feedback Call",
                    Remarks = "Gather feedback regarding the recent deployment rollout.",
                    Status = "Completed",
                    CompletedDate = DateTime.UtcNow.AddDays(-1),
                    AssignedTo = emmaUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                },
                new FollowUp
                {
                    RelatedType = "Lead",
                    RelatedId = 2,
                    FollowUpDate = DateTime.UtcNow.AddDays(-2),
                    FollowUpType = "Email",
                    Subject = "Technical Whitepaper Follow-Up",
                    Remarks = "Follow up on cybersecurity whitepaper sent earlier.",
                    Status = "Missed",
                    AssignedTo = salesUser?.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-4)
                }
            };

            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Activities
        if (!context.Activities.Any())
        {
            var custApex = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var custBlue = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1002");
            var leadNova = context.Leads.FirstOrDefault(l => l.LeadCode == "LEAD-2001");
            var salesUser = await userManager.FindByEmailAsync("sales@acxiom.com");
            var emmaUser = await userManager.FindByEmailAsync("emma@acxiom.com");

            var activities = new List<Activity>
            {
                new Activity
                {
                    ActivityType = "Meeting",
                    Subject = "Architecture Review Session",
                    Description = "Deep-dive session with Apex Global engineering leads to review cloud topology.",
                    ActivityDate = DateTime.UtcNow.AddDays(-2),
                    CustomerId = custApex?.CustomerId,
                    AssignedTo = salesUser?.Id,
                    Status = "Completed"
                },
                new Activity
                {
                    ActivityType = "Call",
                    Subject = "Discovery Call with Nova Energy",
                    Description = "Discussed budget parameters and software deployment timelines.",
                    ActivityDate = DateTime.UtcNow.AddDays(-4),
                    LeadId = leadNova?.LeadId,
                    AssignedTo = salesUser?.Id,
                    Status = "Completed"
                },
                new Activity
                {
                    ActivityType = "Email",
                    Subject = "Proposal v1.3 Sent",
                    Description = "Dispatched updated commercial proposal with discounted volume tier.",
                    ActivityDate = DateTime.UtcNow.AddDays(-5),
                    CustomerId = custApex?.CustomerId,
                    AssignedTo = salesUser?.Id,
                    Status = "Completed"
                },
                new Activity
                {
                    ActivityType = "Task",
                    Subject = "Prepare SOC-2 Compliance Documentation",
                    Description = "Gather security documentation and SLA sheets for compliance sign-off.",
                    ActivityDate = DateTime.UtcNow.AddDays(2),
                    CustomerId = custBlue?.CustomerId,
                    AssignedTo = emmaUser?.Id,
                    Status = "Pending"
                }
            };

            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        // 8. Seed Audit Log baseline entries
        if (!context.AuditLogs.Any())
        {
            var auditEntries = new List<AuditLog>
            {
                new AuditLog
                {
                    UserId = "SYS-INITIALIZE",
                    UserName = "System Setup",
                    Action = "Security",
                    EntityName = "Identity",
                    Result = "Success",
                    Details = "Initialized default roles (Admin, Manager, SalesExecutive) and security policies.",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = "admin@acxiom.com",
                    UserName = "Alexander Wright",
                    Action = "Login",
                    EntityName = "Authentication",
                    Result = "Success",
                    Details = "Administrator logged into AcxiomCRM platform successfully.",
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    IpAddress = "192.168.1.10"
                },
                new AuditLog
                {
                    UserId = "sales@acxiom.com",
                    UserName = "David Miller",
                    Action = "Create",
                    EntityName = "Lead",
                    RecordId = "LEAD-2001",
                    NewValue = "LeadName: Mark Henderson, Company: Nova Energy Grid",
                    Result = "Success",
                    Details = "Created high priority lead from inbound website request.",
                    CreatedDate = DateTime.UtcNow.AddDays(-12),
                    IpAddress = "192.168.1.25"
                },
                new AuditLog
                {
                    UserId = "sales@acxiom.com",
                    UserName = "David Miller",
                    Action = "Update",
                    EntityName = "Opportunity",
                    RecordId = "OPP-101",
                    OldValue = "Stage: Proposal",
                    NewValue = "Stage: Negotiation",
                    Result = "Success",
                    Details = "Advanced Enterprise Cloud Migration deal to Negotiation stage.",
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    IpAddress = "192.168.1.25"
                }
            };

            await context.AuditLogs.AddRangeAsync(auditEntries);
            await context.SaveChangesAsync();
        }
    }
}
