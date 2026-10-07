using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class ActivitiesController(ApplicationDbContext db, IAuditService audit, ValidationService rules) : CrmController<Activity>(db, audit, rules)
{
    protected override string EntityName => "Activity";
    protected override string KeyName => nameof(Activity.ActivityId);
    protected override IQueryable<Activity> WithIncludes(IQueryable<Activity> q) => q.Include(a => a.Customer).Include(a => a.Lead);

    protected override IQueryable<Activity> Filter(IQueryable<Activity> q, string? s, string? status, DateTime? d)
    {
        if (!string.IsNullOrEmpty(s)) q = q.Where(a => a.Subject.Contains(s));
        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status || a.ActivityType == status);
        if (d != null) { var day = d.Value.Date; var next = day.AddDays(1); q = q.Where(a => a.ActivityDate >= day && a.ActivityDate < next); }
        return q;
    }
    protected override Task<List<RuleError>> BusinessErrors(Activity m, Activity? e) => Rules.ActivityAsync(m);
    protected override async Task AddLookups()
    {
        ViewBag.StatusList = new SelectList(Lists.ActivityStatus);
        ViewBag.FilterList = new SelectList(Lists.ActivityTypes.Concat(Lists.ActivityStatus));
        ViewBag.TypeList = new SelectList(Lists.ActivityTypes);
        ViewBag.Customers = new SelectList(await Db.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await Db.Leads.OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
    }
}
