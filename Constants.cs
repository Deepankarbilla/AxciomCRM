namespace AcxiomCRM.Models;

public static class Roles
{
    public const string Admin = "Admin", Manager = "Manager", SalesExecutive = "SalesExecutive";
    public static readonly string[] All = { Admin, Manager, SalesExecutive };
}

public static class Rx
{
    public const string Phone = @"^[6-9]\d{9}$";            // 10-digit Indian mobile
    public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
}

public static class Ids
{
    public static string New(string prefix) => $"{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}

public static class Lists
{
    public static readonly string[] CustomerStatus = { "Active", "Inactive" };
    public static readonly string[] LeadStatus = { "New", "Contacted", "Qualified", "Unqualified", "Lost", "Converted" };
    public static readonly string[] LeadSource = { "Website", "Referral", "Cold Call", "Email Campaign", "Social Media", "Event", "Other" };
    public static readonly string[] Stages = { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
    public static readonly string[] FollowUpTypes = { "Call", "Meeting", "Email", "Task" };
    public static readonly string[] FollowUpStatus = { "Planned", "Completed", "Missed", "Cancelled" };
    public static readonly string[] ActivityTypes = { "Call", "Meeting", "Email", "Task" };
    public static readonly string[] ActivityStatus = { "Open", "Completed", "Cancelled" };

    // Lead status workflow: allowed transitions
    public static readonly Dictionary<string, string[]> LeadFlow = new()
    {
        ["New"] = new[] { "Contacted", "Unqualified", "Lost" },
        ["Contacted"] = new[] { "Qualified", "Unqualified", "Lost" },
        ["Qualified"] = new[] { "Converted", "Lost" },
        ["Unqualified"] = new[] { "Contacted", "Lost" },
        ["Lost"] = Array.Empty<string>(),
        ["Converted"] = Array.Empty<string>()
    };
}
