using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(150)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty; // Authentication, Customer, Lead, Opportunity, FollowUp, User, Role

    [MaxLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(50)]
    public string Result { get; set; } = "Success"; // Success, Failed, Blocked

    public string? Details { get; set; }
}
