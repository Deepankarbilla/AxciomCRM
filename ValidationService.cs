using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public record RuleError(string Field, string Message, bool Conflict = false);

/// Server-side business validation shared by MVC controllers and REST API.
public class ValidationService(ApplicationDbContext db)
{
    public async Task<List<RuleError>> CustomerAsync(Customer c)
    {
        var e = new List<RuleError>();
        if (!Lists.CustomerStatus.Contains(c.Status)) e.Add(new("Status", "Select a valid status."));
        var email = (c.Email ?? "").Trim().ToLower();
        var phone = (c.Phone ?? "").Trim();
        var name = (c.CustomerName ?? "").Trim().ToLower();
        var comp = (c.CompanyName ?? "").Trim().ToLower();
        var id = c.CustomerId;
        if (await db.Customers.AnyAsync(x => x.CustomerId != id && x.Email.ToLower() == email))
            e.Add(new("Email", "A customer with this email already exists.", true));
        if (await db.Customers.AnyAsync(x => x.CustomerId != id && x.Phone == phone))
            e.Add(new("Phone", "A customer with this phone number already exists.", true));
        if (await db.Customers.AnyAsync(x => x.CustomerId != id && x.CustomerName.ToLower() == name && (x.CompanyName ?? "").ToLower() == comp))
            e.Add(new("CustomerName", "Duplicate customer: same name and company already exist.", true));
        return e;
    }

    public List<RuleError> Lead(Lead l, string? oldStatus)
    {
        var e = new List<RuleError>();
        if (!Lists.LeadStatus.Contains(l.Status)) e.Add(new("Status", "Select a valid lead status."));
        else if (l.Status == "Converted" && oldStatus != "Converted") e.Add(new("Status", "A lead can only become Converted through the Convert action."));
        else if (oldStatus != null && oldStatus != l.Status)
        {
            if (!Lists.LeadFlow.TryGetValue(oldStatus, out var allowed) || !allowed.Contains(l.Status))
                e.Add(new("Status", $"Invalid status change from {oldStatus} to {l.Status}."));
        }
        if (!Lists.LeadSource.Contains(l.Source)) e.Add(new("Source", "Select a valid lead source."));
        if (l.ExpectedValue < 0) e.Add(new("ExpectedValue", "Expected value cannot be negative."));
        return e;
    }

    public async Task<List<RuleError>> OpportunityAsync(Opportunity o)
    {
        var e = new List<RuleError>();
        if (!Lists.Stages.Contains(o.Stage)) e.Add(new("Stage", "Select a valid stage."));
        if (o.Probability < 0 || o.Probability > 100) e.Add(new("Probability", "Probability must be between 0 and 100."));
        if (o.Amount < 0) e.Add(new("Amount", "Opportunity Amount cannot be negative."));
        bool active = o.Stage != "Won" && o.Stage != "Lost";
        if (active)
        {
            if (o.Amount <= 0) e.Add(new("Amount", "Opportunity Amount must be greater than 0."));
            if (o.ExpectedCloseDate.Date < DateTime.Today) e.Add(new("ExpectedCloseDate", "Expected Close Date cannot be in the past."));
        }
        if (!await db.Customers.AnyAsync(c => c.CustomerId == o.CustomerId)) e.Add(new("CustomerId", "Select a valid customer."));
        if (o.LeadId != null && !await db.Leads.AnyAsync(l => l.LeadId == o.LeadId)) e.Add(new("LeadId", "Referenced lead does not exist."));
        return e;
    }

    public async Task<List<RuleError>> FollowUpAsync(FollowUp f)
    {
        var e = new List<RuleError>();
        if (!Lists.FollowUpTypes.Contains(f.FollowUpType)) e.Add(new("FollowUpType", "Select a valid follow-up type."));
        if (!Lists.FollowUpStatus.Contains(f.Status)) e.Add(new("Status", "Select a valid status."));
        if (f.CustomerId == null && f.LeadId == null) e.Add(new("", "Select a customer or a lead for this follow-up."));
        if (f.Status == "Planned" && f.FollowUpDate.Date < DateTime.Today) e.Add(new("FollowUpDate", "Follow-up date cannot be earlier than today."));
        e.AddRange(await RefErrors(f.CustomerId, f.LeadId));
        return e;
    }

    public async Task<List<RuleError>> ActivityAsync(Activity a)
    {
        var e = new List<RuleError>();
        if (!Lists.ActivityTypes.Contains(a.ActivityType)) e.Add(new("ActivityType", "Select a valid activity type."));
        if (!Lists.ActivityStatus.Contains(a.Status)) e.Add(new("Status", "Select a valid status."));
        e.AddRange(await RefErrors(a.CustomerId, a.LeadId));
        return e;
    }

    private async Task<List<RuleError>> RefErrors(int? customerId, int? leadId)
    {
        var e = new List<RuleError>();
        if (customerId != null && !await db.Customers.AnyAsync(c => c.CustomerId == customerId)) e.Add(new("CustomerId", "Customer does not exist."));
        if (leadId != null && !await db.Leads.AnyAsync(l => l.LeadId == leadId)) e.Add(new("LeadId", "Lead does not exist."));
        return e;
    }
}
