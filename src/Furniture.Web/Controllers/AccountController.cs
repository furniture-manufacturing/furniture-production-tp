using Furniture.Web.Data;
using Furniture.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

public sealed class AccountController(FurnitureDbContext db, IPasswordHasher<Employee> passwords) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home")
        : View(model: returnUrl);

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(string? login, string? password, string? returnUrl, CancellationToken ct)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(
            e => e.Login == (login ?? "").Trim().ToLowerInvariant() && e.IsActive, ct);
        if (employee is null || string.IsNullOrEmpty(password) || password.Length > 128 ||
            employee.PasswordHash == "not-set" ||
            passwords.VerifyHashedPassword(employee, employee.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Проверьте логин и пароль.");
            return View(model: returnUrl);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, employee.Principal());
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denied() => StatusCode(403, "У вашей учётной записи нет доступа к этому разделу.");
}
