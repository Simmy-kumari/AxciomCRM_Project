using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class UserListViewModel
{
    public List<UserItemDto> Users { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
    public string? StatusFilter { get; set; }

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
}

public class UserItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    [Display(Name = "Full Name *")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid corporate email.")]
    [Display(Name = "Email Address *")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Department / Team")]
    public string? Department { get; set; }

    [Required(ErrorMessage = "Role selection is required.")]
    [Display(Name = "User Role *")]
    public string Role { get; set; } = "SalesExecutive"; // Admin, Manager, SalesExecutive

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character.")]
    [DataType(DataType.Password)]
    [Display(Name = "Initial Password *")]
    public string Password { get; set; } = string.Empty;
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [Display(Name = "Full Name *")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [Display(Name = "Email Address *")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Department / Team")]
    public string? Department { get; set; }

    [Required]
    [Display(Name = "Assigned Role *")]
    public string Role { get; set; } = "SalesExecutive";

    [Display(Name = "Account Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Reset Password (leave empty to keep unchanged)")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
