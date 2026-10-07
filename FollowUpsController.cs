using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class FollowUpsController(ApplicationDbContext db, IAuditService audit, ValidationService rules) : CrmController<FollowUp>(db, audit, rules)
{
    protected override string EntityName => "FollowUp";
    protected override string KeyName => nameof(FollowUp.FollowUpId);
    protected override IQueryable<FollowUp> WithIncludes(IQueryable<FollowUp> q) => q.Include(f => f.Customer).Include(f => f.Lead);

    protected override IQueryable<FollowUp> Filter(IQueryable<FollowUp> q, string? s, string? status, DateTime? d)
    {
        if (!string.IsNullOrEmpty(s)) q = q.Where(f => (f.Customer != null && f.Customer.CustomerName.Contains(s)) || (f.Lead != null && f.Lead.LeadName.Contains(s)) || (f.Remarks != null && f.Remarks.Contains(s)));
        if (!string.IsNullOrEmpty(status)) q = q.Where(f => f.Status == status);
        if (d != null) { var day = d.Value.Date; var next = day.AddDays(1); q = q.Where(f => f.FollowUpDate >= day && f.FollowUpDate < next); }
        return q;
    }
    protected override Task<List<RuleError>> BusinessErrors(FollowUp m, FollowUp? e) => Rules.FollowUpAsync(m);
    protected override async Task AddLookups()
    {
        ViewBag.StatusList = new SelectList(Lists.FollowUpStatus);
        ViewBag.FilterList = ViewBag.StatusList;
        ViewBag.TypeList = new SelectList(Lists.FollowUpTypes);
        ViewBag.Customers = new SelectList(await Db.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await Db.Leads.OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var f = await Db.FollowUps.FindAsync(id);
        if (f == null) return NotFound();
        if (!CanAccess(f)) return Forbid();
        if (f.Status != "Planned") { TempData["Error"] = "Only planned follow-ups can be completed."; return RedirectToAction(nameof(Index)); }
        var old = Snapshot(f);
        f.Status = "Completed";
        await Db.SaveChangesAsync();
        await Audit.LogAsync("Complete", EntityName, id.ToString(), old, f);
        TempData["Success"] = "Follow-up marked as completed.";
        return RedirectToAction(nameof(Index));
    }
}
