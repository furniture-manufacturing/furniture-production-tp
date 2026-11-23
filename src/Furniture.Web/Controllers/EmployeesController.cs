using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public sealed class EmployeesController(FurnitureDbContext db, OrderService orders, IPasswordHasher<Employee> passwords) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => await Directory(new EmployeeForm(), ct);

    [HttpPost]
    public async Task<IActionResult> Create(EmployeeForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Password))
            ModelState.AddModelError(nameof(form.Password), "Укажите пароль длиной от 12 до 128 символов.");
        if (!ModelState.IsValid)
            return await Directory(form, ct);
        try
        {
            await orders.InTransactionAsync(async () =>
            {
                ValidateRoles(form.Roles);
                var login = form.Login.Trim().ToLowerInvariant();
                OrderService.Require(!await db.Employees.AnyAsync(employee => employee.Login == login, ct),
                    "Этот логин уже занят.");
                var employee = new Employee
                {
                    FullName = form.FullName.Trim(), Phone = form.Phone.Trim(), Login = login,
                    Roles = form.Roles.Distinct().ToList()
                };
                employee.PasswordHash = passwords.HashPassword(employee, form.Password!);
                db.Employees.Add(employee);
                await orders.FillQueueAsync(ct);
                return true;
            }, ct);
            TempData["Message"] = "Учётная запись создана.";
            return RedirectToAction(nameof(Index));
        }
        catch (WorkflowException exception)
        {
            ModelState.AddModelError("", exception.Message);
            return await Directory(form, ct);
        }
    }

    [HttpPost]
    public Task<IActionResult> ChangeRoles(int id, EmployeeRole[] roles, CancellationToken ct) =>
        Update(id, employee =>
        {
            ValidateRoles(roles);
            employee.Roles = roles.Distinct().ToList();
        }, ct);

    [HttpPost]
    public Task<IActionResult> ChangeActivity(int id, bool active, CancellationToken ct) =>
        Update(id, employee => employee.IsActive = active, ct);

    private async Task<IActionResult> Update(int id, Action<Employee> change, CancellationToken ct)
    {
        try
        {
            await orders.InTransactionAsync(async () =>
            {
                var employee = await db.Employees.SingleOrDefaultAsync(employee => employee.Id == id, ct);
                OrderService.Require(employee is not null && ModelState.IsValid, "Проверьте параметры учётной записи.");
                var wasAdministrator = employee!.IsActive && employee.Roles.Contains(EmployeeRole.Administrator);
                change(employee);
                if (wasAdministrator && (!employee.IsActive || !employee.Roles.Contains(EmployeeRole.Administrator)))
                    OrderService.Require(await db.Employees.AnyAsync(other => other.Id != id && other.IsActive &&
                        other.Roles.Contains(EmployeeRole.Administrator), ct), "В системе должен оставаться активный администратор.");
                orders.RevokeAssignments(employee, User.EmployeeId());
                await orders.FillQueueAsync(ct);
                return true;
            }, ct);
            TempData["Message"] = "Учётная запись обновлена.";
        }
        catch (WorkflowException exception)
        {
            TempData["Error"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private static void ValidateRoles(EmployeeRole[] roles) =>
        OrderService.Require(roles.Length > 0 && roles.All(Enum.IsDefined), "Выберите хотя бы одну роль.");

    private async Task<IActionResult> Directory(EmployeeForm form, CancellationToken ct)
    {
        ViewBag.Employees = await db.Employees.AsNoTracking().OrderBy(employee => employee.FullName).ToListAsync(ct);
        return View("Index", form);
    }
}
