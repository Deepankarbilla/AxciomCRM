using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcxiomCRM.Controllers;

public class AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, IAuditService audit) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(new LoginVm()); }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var id = vm.UserNameOrEmail.Trim();
        var user = await users.FindByEmailAsync(id) ?? await users.FindByNameAsync(id);
        if (user == null || !user.IsActive)
        {
            await audit.LogAsync("FailedLogin", "Account", null, null, new { Identifier = id, Reason = user == null ? "Unknown user" : "Inactive" });
            ModelState.AddModelError("", "Invalid login attempt.");
            return View(vm);
        }

        var result = await signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await audit.LogAsync("Login", "Account", user.Id, null, new { user.Email }, user.Id);
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
        }
        if (result.IsLockedOut)
        {
            await audit.LogAsync("Lockout", "Account", user.Id, null, new { user.Email }, user.Id);
            ModelState.AddModelError("", "Account locked due to repeated failed attempts. Try again later or contact an administrator.");
            return View(vm);
        }
        await audit.LogAsync("FailedLogin", "Account", user.Id, null, new { user.Email, Reason = "Bad password" }, user.Id);
        ModelState.AddModelError("", "Invalid login attempt.");
        return View(vm);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = new ApplicationUser { UserName = vm.Email.Trim(), Email = vm.Email.Trim(), FullName = vm.FullName.Trim(), IsActive = true };
        var result = await users.CreateAsync(user, vm.Password);   // password policy + hashing by Identity
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }
        await users.AddToRoleAsync(user, Roles.SalesExecutive);   // self-registration is always the least-privileged role
        await audit.LogAsync("Register", "User", user.Id, null, new { user.Email, Role = Roles.SalesExecutive }, user.Id);
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await audit.LogAsync("Logout", "Account", null);
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccessDenied() => View();
}
