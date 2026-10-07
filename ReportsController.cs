using System.Security.Claims;
using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController(ApplicationDbContext db) : Controller
{
    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool Sales => User.IsInRole(Roles.SalesExecutive);

    public async Task<IActionResult> Index()
    {
        var uid = Uid;
        IQueryable<Opportunity> oq = db.Opportunities; IQueryable<Lead> lq = db.Leads; IQueryable<FollowUp> fq = db.FollowUps;
        if (Sales) { oq = oq.Where(x => x.AssignedTo == uid); lq = lq.Where(x => x.AssignedTo == uid); fq = fq.Where(x => x.AssignedTo == uid); }
        var names = await db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        var vm = new ReportVm();

        // Pipeline: stage-wise
        vm.Stages = await oq.GroupBy(o => o.Stage).Select(g => new ReportRow { Label = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount), Weighted = g.Sum(x => x.Amount * x.Probability / 100) }).ToListAsync();
        // Pipeline: owner-wise (open only)
        var owners = await oq.Where(o => o.Status == "Open").GroupBy(o => o.AssignedTo).Select(g => new ReportRow { Label = g.Key ?? "", Count = g.Count(), Amount = g.Sum(x => x.Amount), Weighted = g.Sum(x => x.Amount * x.Probability / 100) }).ToListAsync();
        foreach (var r in owners) r.Label = names.GetValueOrDefault(r.Label, "Unassigned");
        vm.Owners = owners;
        // Lead conversion by source
        vm.LeadSources = await lq.GroupBy(l => l.Source).Select(g => new ReportRow { Label = g.Key, Count = g.Count(), Converted = g.Count(x => x.Status == "Converted") }).ToListAsync();
        // Follow-ups
        var today = DateTime.Today;
        var fu = await fq.GroupBy(f => f.Status).Select(g => new ReportRow { Label = g.Key, Count = g.Count() }).ToListAsync();
        fu.Add(new ReportRow { Label = "Overdue (planned & past)", Count = await fq.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < today) });
        vm.FollowUps = fu;
        // User activity (Admin/Manager)
        if (!Sales)
        {
            vm.ShowUserActivity = true;
            var since = DateTime.UtcNow.AddDays(-30);
            var ua = await db.AuditLogs.Where(a => a.CreatedDate >= since && a.UserId != null).GroupBy(a => a.UserId).Select(g => new ReportRow { Label = g.Key!, Count = g.Count() }).ToListAsync();
            foreach (var r in ua) r.Label = names.GetValueOrDefault(r.Label, r.Label);
            vm.UserActivity = ua.OrderByDescending(r => r.Count).ToList();
        }
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ExportOpportunities()
    {
        var rows = await db.Opportunities.Include(o => o.Customer).OrderBy(o => o.OpportunityId).ToListAsync();
        var sb = new StringBuilder("Id,Name,Customer,Stage,Amount,Probability,ExpectedCloseDate,Status\n");
        foreach (var o in rows)
            sb.AppendLine(string.Join(",", Csv(o.OpportunityId.ToString()), Csv(o.OpportunityName), Csv(o.Customer?.CustomerName ?? ""), Csv(o.Stage), Csv(o.Amount.ToString("0.00")), Csv(o.Probability.ToString()), Csv(o.ExpectedCloseDate.ToString("yyyy-MM-dd")), Csv(o.Status)));
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "opportunities.csv");
    }

    // CSV-injection safe
    private static string Csv(string v)
    {
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }
}
