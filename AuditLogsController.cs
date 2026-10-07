using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = Roles.Admin)]   // read-only: there is intentionally no edit/delete action
public class AuditLogsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? userId, [FromQuery(Name = "act")] string? actionName, string? entity, [FromQuery(Name = "from")] DateTime? fromDate, [FromQuery(Name = "to")] DateTime? toDate, int page = 1)
    {
        var q = db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(userId)) q = q.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(actionName)) q = q.Where(a => a.Action == actionName);
        if (!string.IsNullOrEmpty(entity)) q = q.Where(a => a.EntityName == entity);
        if (fromDate != null) { var f = fromDate.Value.Date.ToUniversalTime(); q = q.Where(a => a.CreatedDate >= f); }
        if (toDate != null) { var t = toDate.Value.Date.AddDays(1).ToUniversalTime(); q = q.Where(a => a.CreatedDate < t); }
        const int size = 20;
        var total = await q.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        page = Math.Clamp(page, 1, pages);
        var items = await q.OrderByDescending(a => a.AuditLogId).Skip((page - 1) * size).Take(size).ToListAsync();
        ViewBag.Page = page; ViewBag.Pages = pages; ViewBag.Total = total;
        ViewBag.UserNames = await db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.Actions = await db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
        ViewBag.Entities = await db.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(a => a).ToListAsync();
        return View(items);
    }
}
