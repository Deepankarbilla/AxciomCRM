using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class OpportunitiesController(ApplicationDbContext db, IAuditService audit, ValidationService rules) : CrmController<Opportunity>(db, audit, rules)
{
    protected override string EntityName => "Opportunity";
    protected override string KeyName => nameof(Opportunity.OpportunityId);
    protected override IQueryable<Opportunity> WithIncludes(IQueryable<Opportunity> q) => q.Include(o => o.Customer);

    protected override IQueryable<Opportunity> Filter(IQueryable<Opportunity> q, string? s, string? status, DateTime? d)
    {
        if (!string.IsNullOrEmpty(s)) q = q.Where(o => o.OpportunityName.Contains(s) || (o.Customer != null && o.Customer.CustomerName.Contains(s)));
        if (!string.IsNullOrEmpty(status)) q = q.Where(o => o.Stage == status || o.Status == status);
        return q;
    }
    protected override void Normalize(Opportunity o)
    {
        o.OpportunityName = (o.OpportunityName ?? "").Trim();
        o.Status = o.Stage == "Won" ? "Won" : o.Stage == "Lost" ? "Lost" : "Open";
        if (o.Stage == "Won") o.Probability = 100;
        if (o.Stage == "Lost") o.Probability = 0;
    }
    protected override void OnCreate(Opportunity o) { o.CreatedDate = DateTime.Now; }
    protected override void KeepServerFields(Opportunity m, Opportunity e) { m.CreatedDate = e.CreatedDate; m.LeadId = e.LeadId; }
    protected override Task<List<RuleError>> BusinessErrors(Opportunity m, Opportunity? e) => Rules.OpportunityAsync(m);
    protected override async Task AddLookups()
    {
        ViewBag.StageList = new SelectList(Lists.Stages);
        ViewBag.FilterList = new SelectList(Lists.Stages.Append("Open"));
        ViewBag.Customers = new SelectList(await Db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
    }
}
