using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Models;

public interface IOwned { int Id { get; } string? AssignedTo { get; set; } }

// Passwords are managed by ASP.NET Core Identity (hashed). No custom password table.
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(Phone), IsUnique = true)]
public class Customer : IOwned
{
    [Key] public int CustomerId { get; set; }
    [StringLength(20)] public string CustomerCode { get; set; } = "";
    [Required(ErrorMessage = "Customer Name is required."), StringLength(100)] public string CustomerName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), StringLength(150)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [RegularExpression(Rx.Email, ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Phone is required."), StringLength(15)]
    [RegularExpression(Rx.Phone, ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";
    [StringLength(100)] public string? CompanyName { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(60)] public string? City { get; set; }
    [StringLength(60)] public string? State { get; set; }
    [Required] public string Status { get; set; } = "Active";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }
    public string? AssignedTo { get; set; }
    [NotMapped] public int Id => CustomerId;
}

public class Lead : IOwned
{
    [Key] public int LeadId { get; set; }
    [StringLength(20)] public string LeadCode { get; set; } = "";
    [Required(ErrorMessage = "Lead Name is required."), StringLength(100)] public string LeadName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), StringLength(150)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [RegularExpression(Rx.Email, ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Phone is required."), StringLength(15)]
    [RegularExpression(Rx.Phone, ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";
    [StringLength(100)] public string? CompanyName { get; set; }
    [Required(ErrorMessage = "Source is required.")] public string Source { get; set; } = "Website";
    [Required(ErrorMessage = "Status is required.")] public string Status { get; set; } = "New";
    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Expected value must be between 0 and 100,000,000.")]
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? AssignedTo { get; set; }
    [NotMapped] public int Id => LeadId;
}

public class Opportunity : IOwned
{
    [Key] public int OpportunityId { get; set; }
    [Required(ErrorMessage = "Opportunity Name is required."), StringLength(150)] public string OpportunityName { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")] public int CustomerId { get; set; }
    public int? LeadId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "10000000000", ErrorMessage = "Opportunity Amount cannot be negative.")]
    public decimal Amount { get; set; }
    [Required] public string Stage { get; set; } = "Qualification";
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")] public int Probability { get; set; } = 25;
    [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? AssignedTo { get; set; }
    [BindNever, ValidateNever, JsonIgnore] public Customer? Customer { get; set; }
    [NotMapped] public int Id => OpportunityId;
}

public class FollowUp : IOwned
{
    [Key] public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;
    [Required(ErrorMessage = "Follow-up type is required.")] public string FollowUpType { get; set; } = "Call";
    [StringLength(500)] public string? Remarks { get; set; }
    [Required(ErrorMessage = "Status is required.")] public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
    [BindNever, ValidateNever, JsonIgnore] public Customer? Customer { get; set; }
    [BindNever, ValidateNever, JsonIgnore] public Lead? Lead { get; set; }
    [NotMapped] public int Id => FollowUpId;
}

public class Activity : IOwned
{
    [Key] public int ActivityId { get; set; }
    [Required(ErrorMessage = "Activity type is required.")] public string ActivityType { get; set; } = "Call";
    [Required(ErrorMessage = "Subject is required."), StringLength(150)] public string Subject { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime ActivityDate { get; set; } = DateTime.Today;
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public string? AssignedTo { get; set; }
    [Required(ErrorMessage = "Status is required.")] public string Status { get; set; } = "Open";
    [BindNever, ValidateNever, JsonIgnore] public Customer? Customer { get; set; }
    [BindNever, ValidateNever, JsonIgnore] public Lead? Lead { get; set; }
    [NotMapped] public int Id => ActivityId;
}

public class AuditLog
{
    [Key] public int AuditLogId { get; set; }
    public string? UserId { get; set; }
    [StringLength(50)] public string Action { get; set; } = "";
    [StringLength(50)] public string EntityName { get; set; } = "";
    [StringLength(50)] public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [StringLength(60)] public string? IpAddress { get; set; }
}
