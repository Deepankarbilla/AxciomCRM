using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.Dtos;

public class LoginDto
{
    [Required] public string UserNameOrEmail { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

// ---------- Customers ----------
public class CustomerInputDto
{
    [Required, StringLength(100)] public string CustomerName { get; set; } = "";
    [Required, StringLength(150), RegularExpression(Rx.Email, ErrorMessage = "Enter a valid email address.")] public string Email { get; set; } = "";
    [Required, StringLength(15), RegularExpression(Rx.Phone, ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
    [StringLength(100)] public string? CompanyName { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(60)] public string? City { get; set; }
    [StringLength(60)] public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? AssignedTo { get; set; }
}
public class CustomerDto : CustomerInputDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}

// ---------- Leads ----------
public class LeadInputDto
{
    [Required, StringLength(100)] public string LeadName { get; set; } = "";
    [Required, StringLength(150), RegularExpression(Rx.Email, ErrorMessage = "Enter a valid email address.")] public string Email { get; set; } = "";
    [Required, StringLength(15), RegularExpression(Rx.Phone, ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
    [StringLength(100)] public string? CompanyName { get; set; }
    [Required] public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    [Range(typeof(decimal), "0", "100000000")] public decimal ExpectedValue { get; set; }
    public string? AssignedTo { get; set; }
}
public class LeadDto : LeadInputDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}

// ---------- Opportunities ----------
public class OpportunityInputDto
{
    [Required, StringLength(150)] public string OpportunityName { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")] public int CustomerId { get; set; }
    public int? LeadId { get; set; }
    [Range(typeof(decimal), "0", "10000000000", ErrorMessage = "Opportunity Amount cannot be negative.")] public decimal Amount { get; set; }
    [Required] public string Stage { get; set; } = "Qualification";
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")] public int Probability { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string? AssignedTo { get; set; }
}
public class OpportunityDto : OpportunityInputDto
{
    public int OpportunityId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}
