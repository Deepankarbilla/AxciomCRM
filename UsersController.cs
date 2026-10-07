using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsersController(UserManager<ApplicationUser> um, ApplicationDbContext db, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var q = um.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim(); q = q.Where(u => u.FullName.Contains(s) || u.Email!.Contains(s)); }
        const int size = 10;
        var total = await q.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        page = Math.Clamp(page, 1, pages);
        var list = await q.OrderBy(u => u.FullName).Skip((page - 1) * size).Take(size).ToListAsync();
        var rows = new List<UserRow>();
        foreach (var u in list)
            rows.Add(new UserRow { User = u, Role = (await um.GetRolesAsync(u)).FirstOrDefault() ?? "-", Locked = await um.IsLockedOutAsync(u) });
        ViewBag.Page = page; ViewBag.Pages = pages;
        return View(rows);
    }

    [HttpGet] public IActionResult Create() => View(new UserCreateVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVm vm)
    {
        if (!Roles.All.Contains(vm.Role)) ModelState.AddModelError("Role", "Select a valid role.");
        if (!ModelState.IsValid) return View(vm);
        var u = new ApplicationUser { UserName = vm.Email.Trim(), Email = vm.Email.Trim(), FullName = vm.FullName.Trim(), IsActive = vm.IsActive, EmailConfirmed = true };
        var r = await um.CreateAsync(u, vm.Password);
        if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError("", e.Description); return View(vm); }
        await um.AddToRoleAsync(u, vm.Role);
        await audit.LogAsync("Create", "User", u.Id, null, new { u.Email, u.FullName, Role = vm.Role, u.IsActive });
        TempData["Success"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        return View(new UserEditVm { Id = u.Id, Email = u.Email!, FullName = u.FullName, IsActive = u.IsActive, Role = (await um.GetRolesAsync(u)).FirstOrDefault() ?? Roles.SalesExecutive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm vm)
    {
        if (!Roles.All.Contains(vm.Role)) ModelState.AddModelError("Role", "Select a valid role.");
        var u = await um.FindByIdAsync(vm.Id);
        if (u == null) return NotFound();
        vm.Email = u.Email!;
        var me = um.GetUserId(User);
        if (u.Id == me && (!vm.IsActive || vm.Role != Roles.Admin)) ModelState.AddModelError("", "You cannot deactivate yourself or remove your own Admin role.");
        if (!ModelState.IsValid) return View(vm);

        var oldRoles = await um.GetRolesAsync(u);
        var oldState = new { u.FullName, Role = oldRoles.FirstOrDefault(), u.IsActive };
        u.FullName = vm.FullName.Trim(); u.IsActive = vm.IsActive;
        await um.UpdateAsync(u);
        if (!oldRoles.Contains(vm.Role))
        {
            await um.RemoveFromRolesAsync(u, oldRoles);
            await um.AddToRoleAsync(u, vm.Role);
            await audit.LogAsync("RoleChange", "User", u.Id, new { Role = oldRoles.FirstOrDefault() }, new { Role = vm.Role });
        }
        if (oldState.IsActive != vm.IsActive) await um.UpdateSecurityStampAsync(u);   // invalidates existing sessions
        await audit.LogAsync("Update", "User", u.Id, oldState, new { u.FullName, Role = vm.Role, u.IsActive });
        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var u = await um.FindByIdAsync(id);
        return u == null ? NotFound() : View(new ResetPasswordVm { Id = u.Id, Email = u.Email! });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
    {
        var u = await um.FindByIdAsync(vm.Id);
        if (u == null) return NotFound();
        vm.Email = u.Email!;
        if (!ModelState.IsValid) return View(vm);
        var token = await um.GeneratePasswordResetTokenAsync(u);
        var r = await um.ResetPasswordAsync(u, token, vm.NewPassword);
        if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError("", e.Description); return View(vm); }
        await audit.LogAsync("PasswordReset", "User", u.Id, null, new { u.Email });   // never log the password
        TempData["Success"] = "Password reset.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var u = await um.FindByIdAsync(id);
        if (u == null) return NotFound();
        await um.SetLockoutEndDateAsync(u, null);
        await um.ResetAccessFailedCountAsync(u);
        await audit.LogAsync("Unlock", "User", u.Id, null, new { u.Email });
        TempData["Success"] = "Account unlocked.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> RolesPermissions()
    {
        var counts = new Dictionary<string, int>();
        foreach (var r in Roles.All) counts[r] = (await um.GetUsersInRoleAsync(r)).Count;
        return View(counts);
    }
}
