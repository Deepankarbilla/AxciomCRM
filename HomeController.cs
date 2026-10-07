using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? period, [FromQuery(Name = "from")] DateTime? fromDate, [FromQuery(Name = "to")] DateTime? toDate)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        bool sales = User.IsInRole(Roles.SalesExecutive);
        var today = DateTime.Today;

        DateTime? start = null, end = null;
        switch (period)
        {
            case "today": start = today; end = today.AddDays(1); break;
            case "week": start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); end = start.Value.AddDays(7); break;
            case "month": start = new DateTime(today.Year, today.Month, 1); end = start.Value.AddMonths(1); break;
            case "custom": start = fromDate?.Date; end = toDate?.Date.AddDays(1); break;
        }

        IQueryable<Customer> cq = db.Customers; IQueryable<Lead> lq = db.Leads;
        IQueryable<Opportunity> oq = db.Opportunities; IQueryable<FollowUp> fq = db.FollowUps;
        if (sales) { cq = cq.Where(x => x.AssignedTo == uid); lq = lq.Where(x => x.AssignedTo == uid); oq = oq.Where(x => x.AssignedTo == uid); fq = fq.Where(x => x.AssignedTo == uid); }
        if (start != null) { cq = cq.Where(x => x.CreatedDate >= start); lq = lq.Where(x => x.CreatedDate >= start); oq = oq.Where(x => x.CreatedDate >= start); }
        if (end != null) { cq = cq.Where(x => x.CreatedDate < end); lq = lq.Where(x => x.CreatedDate < end); oq = oq.Where(x => x.CreatedDate < end); }

        var vm = new DashboardVm { Period = period ?? "", From = fromDate, To = toDate };
        vm.TotalCustomers = await cq.CountAsync();
        vm.TotalLeads = await lq.CountAsync();
        vm.OpenLeads = await lq.CountAsync(l => l.Status != "Lost" && l.Status != "Converted" && l.Status != "Unqualified");
        vm.TotalOpps = await oq.CountAsync();
        vm.OpenOpps = await oq.CountAsync(o => o.Status == "Open");
        vm.WonOpps = await oq.CountAsync(o => o.Status == "Won");
        vm.LostOpps = await oq.CountAsync(o => o.Status == "Lost");
        vm.PipelineValue = await oq.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0;
        vm.WeightedPipeline = await oq.Where(o => o.Status == "Open").SumAsync(o => (decimal?)(o.Amount * o.Probability / 100)) ?? 0;
        vm.PendingFollowUps = await fq.CountAsync(f => f.Status == "Planned");
        vm.OverdueFollowUps = await fq.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < today);

        var ls = await lq.GroupBy(l => l.Status).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C);
        foreach (var s in new[] { "New", "Contacted", "Qualified", "Lost", "Converted" }) { vm.LeadLabels.Add(s); vm.LeadCounts.Add(ls.GetValueOrDefault(s)); }
        var os = await oq.GroupBy(o => o.Stage).Select(g => new { g.Key, C = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.C);
        foreach (var s in Lists.Stages) { vm.StageLabels.Add(s); vm.StageCounts.Add(os.GetValueOrDefault(s)); }

        var won = await oq.Where(o => o.Stage == "Won").Select(o => new { o.Amount, o.ExpectedCloseDate }).ToListAsync();
        for (int i = 5; i >= 0; i--)
        {
            var m = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
            vm.MonthLabels.Add(m.ToString("MMM yyyy"));
            vm.MonthTotals.Add(won.Where(w => w.ExpectedCloseDate.Year == m.Year && w.ExpectedCloseDate.Month == m.Month).Sum(w => w.Amount));
        }

        if (User.IsInRole(Roles.Admin))
        {
            vm.ShowAdmin = true;
            var now = DateTimeOffset.UtcNow; var since = DateTime.UtcNow.AddHours(-24);
            vm.TotalUsers = await db.Users.CountAsync();
            vm.LockedUsers = await db.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now);
            vm.FailedLogins24h = await db.AuditLogs.CountAsync(a => a.Action == "FailedLogin" && a.CreatedDate >= since);
        }
        return View(vm);
    }

    [AllowAnonymous] public IActionResult Error() => View();
}
