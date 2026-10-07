using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class LoginVm
{
    [Required(ErrorMessage = "Username or email is required.")] public string UserNameOrEmail { get; set; } = "";
    [Required(ErrorMessage = "Password is required."), DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class RegisterVm
{
    [Required(ErrorMessage = "Full name is required."), StringLength(100)] public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")] public string ConfirmPassword { get; set; } = "";
}

public class UserCreateVm : RegisterVm
{
    [Required] public string Role { get; set; } = Roles.SalesExecutive;
    public bool IsActive { get; set; } = true;
}

public class UserEditVm
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Full name is required."), StringLength(100)] public string FullName { get; set; } = "";
    [Required] public string Role { get; set; } = Roles.SalesExecutive;
    public bool IsActive { get; set; }
}

public class ResetPasswordVm
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string NewPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")] public string ConfirmPassword { get; set; } = "";
}

public class UserRow { public ApplicationUser User { get; set; } = null!; public string Role { get; set; } = ""; public bool Locked { get; set; } }

public class DashboardVm
{
    public string Period { get; set; } = "";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalCustomers, TotalLeads, OpenLeads, TotalOpps, OpenOpps, WonOpps, LostOpps, PendingFollowUps, OverdueFollowUps;
    public decimal PipelineValue, WeightedPipeline;
    public int TotalUsers, LockedUsers, FailedLogins24h;
    public bool ShowAdmin;
    public List<string> LeadLabels = new(); public List<int> LeadCounts = new();
    public List<string> StageLabels = new(); public List<int> StageCounts = new();
    public List<string> MonthLabels = new(); public List<decimal> MonthTotals = new();
}

public class ReportRow { public string Label { get; set; } = ""; public int Count { get; set; } public int Converted { get; set; } public decimal Amount { get; set; } public decimal Weighted { get; set; } }
public class ReportVm
{
    public List<ReportRow> Stages = new(), Owners = new(), LeadSources = new(), FollowUps = new(), UserActivity = new();
    public bool ShowUserActivity;
}
