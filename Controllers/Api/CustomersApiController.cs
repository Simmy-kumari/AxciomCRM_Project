using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
[Produces("application/json")]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomersApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                     c.Email.ToLower().Contains(term) ||
                                     c.CustomerCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await query
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerId = c.OwnerId
            })
            .ToListAsync();

        return Ok(customers);
    }

    // GET /api/customers/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        return Ok(new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            OwnerId = c.OwnerId
        });
    }

    // POST /api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate uniqueness
        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new { message = "A customer with this email address already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
        {
            return Conflict(new { message = "A customer with this phone number already exists." });
        }

        int nextId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 1000) + 1;

        var customer = new Customer
        {
            CustomerCode = $"CUST-{nextId}",
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            OwnerId = dto.OwnerId,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            userId: User.Identity?.Name ?? "API",
            userName: User.Identity?.Name ?? "API User",
            action: "Create",
            entityName: "Customer",
            recordId: customer.CustomerCode,
            details: "Created via REST API endpoint.");

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            OwnerId = customer.OwnerId
        };

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
    }

    // PUT /api/customers/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower() && c.CustomerId != id))
        {
            return Conflict(new { message = "Another customer already has this email address." });
        }

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = dto.Email.Trim().ToLower();
        customer.Phone = dto.Phone.Trim();
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;
        customer.OwnerId = dto.OwnerId;
        customer.Notes = dto.Notes;
        customer.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Customer updated successfully." });
    }

    // DELETE /api/customers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found." });

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Customer deleted successfully." });
    }
}
