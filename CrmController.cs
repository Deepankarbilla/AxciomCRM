using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

/// Generic CRUD controller with role-based record scoping, server-side validation and audit logging.
[Authorize]
public abstract class CrmController<T> : Controller where T : class, IOwned, new()
{
    protected readonly ApplicationDbContext Db;
    protected readonly IAuditService Audit;
    protected readonly ValidationService Rules;

    protected CrmController(ApplicationDbContext db, IAuditService audit, ValidationService rules)
    { Db = db; Audit = audit; Rules = rules; }

    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool IsSales => User.IsInRole(Roles.SalesExecutive);
    protected abstract string EntityName { get; }
    protected abstract string KeyName { get; }

    // ---- hooks ----
    protected virtual IQueryable<T> WithIncludes(IQueryable<T> q) => q;
    protected virtual IQueryable<T> Filter(IQueryable<T> q, string? search, string? status, DateTime? date) => q;
    protected virtual void Normalize(T m) { }
    protected virtual void OnCreate(T m) { }
    protected virtual void KeepServerFields(T m, T existing) { }
    protected virtual Task<List<RuleError>> BusinessErrors(T m, T? existing) => Task.FromResult(new List<RuleError>());
    protected virtual Task PerformDelete(T e) { Db.Set<T>().Remove(e); return Task.CompletedTask; }
    protected virtual Task AddLookups() => Task.CompletedTask;

    // ---- helpers ----
    protected static string Snapshot(object o) => JsonOpts.Ser(o)!;
    protected bool CanAccess(T e) => !IsSales || e.AssignedTo == CurrentUserId;

    protected IQueryable<T> Scoped()
    {
        IQueryable<T> q = Db.Set<T>();
        if (IsSales) { var uid = CurrentUserId; q = q.Where(x => EF.Property<string>(x, "AssignedTo") == uid); }
        return q;
    }

    protected async Task LoadLookups()
    {
        var users = await Db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName }).ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "FullName");
        ViewBag.UserNames = await Db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        await AddLookups();
    }

    private async Task<bool> Validate(T m, T? existing)
    {
        if (ModelState.IsValid)
        {
            foreach (var e in await BusinessErrors(m, existing)) ModelState.AddModelError(e.Field, e.Message);
            if (string.IsNullOrEmpty(m.AssignedTo)) m.AssignedTo = CurrentUserId;
            if (!IsSales && !await Db.Users.AnyAsync(u => u.Id == m.AssignedTo && u.IsActive))
                ModelState.AddModelError("AssignedTo", "Select a valid active user.");
        }
        return ModelState.IsValid;
    }

    private async Task<bool> TrySave()
    {
        try { await Db.SaveChangesAsync(); return true; }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "The record could not be saved. A record with the same unique value may already exist.");
            return false;
        }
    }

    // ---- actions ----
    public virtual async Task<IActionResult> Index(string? search, string? status, string? assigned, DateTime? date, int page = 1)
    {
        var key = KeyName;
        await LoadLookups();
        var q = Filter(WithIncludes(Scoped()), search?.Trim(), status, date);
        if (!IsSales && !string.IsNullOrEmpty(assigned)) q = q.Where(x => EF.Property<string>(x, "AssignedTo") == assigned);
        const int size = 10;
        var total = await q.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        page = Math.Clamp(page, 1, pages);
        var items = await q.OrderByDescending(x => EF.Property<int>(x, key)).Skip((page - 1) * size).Take(size).ToListAsync();
        ViewBag.Page = page; ViewBag.Pages = pages; ViewBag.Total = total;
        return View(items);
    }

    public virtual async Task<IActionResult> Details(int id)
    {
        var key = KeyName;
        var e = await WithIncludes(Db.Set<T>()).FirstOrDefaultAsync(x => EF.Property<int>(x, key) == id);
        if (e == null) return NotFound();
        if (!CanAccess(e)) return Forbid();
        await LoadLookups();
        return View(e);
    }

    [HttpGet]
    public virtual async Task<IActionResult> Create() { await LoadLookups(); return View(new T()); }

    [HttpPost, ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Create(T model)
    {
        if (IsSales) model.AssignedTo = CurrentUserId;
        Normalize(model); OnCreate(model);
        Db.Entry(model).Property(KeyName).CurrentValue = 0;   // never trust a posted key
        if (!await Validate(model, null)) { await LoadLookups(); return View(model); }
        Db.Add(model);
        if (!await TrySave()) { Db.Entry(model).State = EntityState.Detached; await LoadLookups(); return View(model); }
        await Audit.LogAsync("Create", EntityName, model.Id.ToString(), null, model);
        TempData["Success"] = $"{EntityName} created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public virtual async Task<IActionResult> Edit(int id)
    {
        var e = await Db.Set<T>().FindAsync(id);
        if (e == null) return NotFound();
        if (!CanAccess(e)) return Forbid();
        await LoadLookups();
        return View(e);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> Edit(int id, T model)
    {
        if (id != model.Id) return BadRequest();
        var existing = await Db.Set<T>().FindAsync(id);
        if (existing == null) return NotFound();
        if (!CanAccess(existing)) return Forbid();
        if (IsSales) model.AssignedTo = existing.AssignedTo;
        KeepServerFields(model, existing); Normalize(model);
        if (!await Validate(model, existing)) { await LoadLookups(); return View(model); }
        var old = Snapshot(existing);
        Db.Entry(existing).CurrentValues.SetValues(model);
        if (!await TrySave()) { await LoadLookups(); return View(model); }
        await Audit.LogAsync("Update", EntityName, id.ToString(), old, existing);
        TempData["Success"] = $"{EntityName} updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin,Manager")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var key = KeyName;
        var e = await WithIncludes(Db.Set<T>()).FirstOrDefaultAsync(x => EF.Property<int>(x, key) == id);
        if (e == null) return NotFound();
        await LoadLookups();
        return View(e);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin,Manager")]
    public virtual async Task<IActionResult> DeleteConfirmed(int id)
    {
        var e = await Db.Set<T>().FindAsync(id);
        if (e == null) return NotFound();
        var old = Snapshot(e);
        await PerformDelete(e);
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException) { TempData["Error"] = "This record cannot be deleted because other records depend on it."; return RedirectToAction(nameof(Index)); }
        await Audit.LogAsync("Delete", EntityName, id.ToString(), old, null);
        TempData["Success"] = $"{EntityName} deleted.";
        return RedirectToAction(nameof(Index));
    }
}
