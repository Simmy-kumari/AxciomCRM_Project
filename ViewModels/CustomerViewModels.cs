using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class CustomerListViewModel
{
    public List<CustomerItemDto> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    // Summary counts
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalCount { get; set; }
}

public class CustomerItemDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int ActiveDealsCount { get; set; }
    public decimal TotalDealsValue { get; set; }
}

public class CustomerFormViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Contact Name *")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Business Email *")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number (e.g. +1 555-019-2834).")]
    [Display(Name = "Phone Number *")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Company / Organization Name")]
    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Display(Name = "Street Address")]
    [StringLength(250)]
    public string? Address { get; set; }

    [Display(Name = "City")]
    [StringLength(100)]
    public string? City { get; set; }

    [Display(Name = "State / Province")]
    [StringLength(100)]
    public string? State { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Active";

    [Display(Name = "Assigned Sales Executive")]
    public string? OwnerId { get; set; }

    [Display(Name = "Account Notes & Strategy")]
    public string? Notes { get; set; }
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = default!;
    public string? OwnerName { get; set; }
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
