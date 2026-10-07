using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class LeadsController(ApplicationDbContext db, IAuditService audit, ValidationService rules) : CrmController<Lead>(db, audit, rules)
{
    protected override string EntityName => "Lead";
    protected override string KeyName => nameof(Lead.LeadId);

    protected override IQueryable<Lead> Filter(IQueryable<Lead> q, string? s, string? status, DateTime? d)
    {
        if (!string.IsNullOrEmpty(s)) q = q.Where(l => l.LeadName.Contains(s) || l.Email.Contains(s) || (l.CompanyName != null && l.CompanyName.Contains(s)));
        if (!string.IsNullOrEmpty(status)) q = q.Where(l => l.Status == status);
        return q;
    }
    protected override void Normalize(Lead l) { l.LeadName = (l.LeadName ?? "").Trim(); l.Email = (l.Email ?? "").Trim(); l.Phone = (l.Phone ?? "").Trim(); }
    protected override void OnCreate(Lead l) { l.LeadCode = Ids.New("LD"); l.CreatedDate = DateTime.Now; }
    protected override void KeepServerFields(Lead m, Lead e) { m.LeadCode = e.LeadCode; m.CreatedDate = e.CreatedDate; }
    protected override Task<List<RuleError>> BusinessErrors(Lead m, Lead? e) => Task.FromResult(Rules.Lead(m, e?.Status));
    protected override Task AddLookups()
    {
        ViewBag.StatusList = new SelectList(Lists.LeadStatus);
        ViewBag.FilterList = ViewBag.StatusList;
        ViewBag.SourceList = new SelectList(Lists.LeadSource);
        return Task.CompletedTask;
    }

    // Lead -> Customer + Opportunity conversion
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await Db.Leads.FindAsync(id);
        if (lead == null) return NotFound();
        if (!CanAccess(lead)) return Forbid();
        if (lead.Status != "Qualified") { TempData["Error"] = "Only Qualified leads can be converted."; return RedirectToAction(nameof(Details), new { id }); }

        await using var tx = await Db.Database.BeginTransactionAsync();
        var email = lead.Email.ToLower(); var phone = lead.Phone;
        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email || c.Phone == phone);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerCode = Ids.New("CUS"), CustomerName = lead.LeadName, Email = lead.Email, Phone = lead.Phone,
                CompanyName = lead.CompanyName, Status = "Active", CreatedBy = CurrentUserId, AssignedTo = lead.AssignedTo ?? CurrentUserId
            };
            Db.Customers.Add(customer);
            await Db.SaveChangesAsync();
            await Audit.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null, customer);
        }
        var old = Snapshot(lead);
        lead.Status = "Converted";
        var opp = new Opportunity
        {
            OpportunityName = $"{lead.LeadName} - Opportunity", CustomerId = customer.CustomerId, LeadId = lead.LeadId,
            Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 1, Stage = "Qualification", Probability = 25, Status = "Open",
            ExpectedCloseDate = DateTime.Today.AddDays(30), AssignedTo = lead.AssignedTo ?? CurrentUserId
        };
        Db.Opportunities.Add(opp);
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(), null, opp);
        await Audit.LogAsync("Convert", "Lead", id.ToString(), old, lead);
        await tx.CommitAsync();
        TempData["Success"] = "Lead converted to customer and opportunity.";
        return RedirectToAction("Details", "Opportunities", new { id = opp.OpportunityId });
    }
}
