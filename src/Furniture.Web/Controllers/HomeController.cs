using System.Diagnostics;
using Furniture.Web.Data;
using Furniture.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

public sealed class HomeController(FurnitureDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (!User.IsInRole(nameof(EmployeeRole.Administrator)))
            return RedirectToAction(nameof(AssignmentsController.Index), "Assignments");
        var today = WorkshopDates.Today;
        var first = from ?? new DateOnly(today.Year, today.Month, 1);
        var last = to ?? today;
        if (first > last || last == DateOnly.MaxValue || first.Year < 2)
            return BadRequest("Проверьте период статистики.");
        var start = WorkshopDates.Start(first);
        var end = WorkshopDates.Start(last.AddDays(1));

        var rows = await db.Orders.AsNoTracking().Select(order => new
        {
            IsSubOrder = order.ParentOrderId.HasValue,
            order.Status,
            Overdue = order.PlannedCompletionDate < today && order.Status != OrderStatus.Completed &&
                order.Status != OrderStatus.Cancelled,
            Waiting = order.Stages.Any(stage => stage.Status == StageStatus.WaitingForAssignee),
            Debt = order.Status != OrderStatus.Cancelled && order.AgreedCost.HasValue
                ? order.AgreedCost.Value - order.Payments.Sum(payment =>
                    payment.Type == PaymentType.Receipt ? payment.Amount : -payment.Amount)
                : 0,
            Receipts = order.Payments.Where(payment => payment.Type == PaymentType.Receipt &&
                payment.Date >= start && payment.Date < end).Sum(payment => payment.Amount),
            Refunds = order.Payments.Where(payment => payment.Type == PaymentType.Refund &&
                payment.Date >= start && payment.Date < end).Sum(payment => payment.Amount)
        }).ToListAsync(ct);

        DashboardGroup Group(bool isSubOrder)
        {
            var group = rows.Where(row => row.IsSubOrder == isSubOrder).ToList();
            return new DashboardGroup(group.GroupBy(row => row.Status).ToDictionary(status => status.Key, status => status.Count()),
                group.Count(row => row.Overdue), group.Count(row => row.Waiting), group.Sum(row => row.Receipts),
                group.Sum(row => row.Refunds), group.Sum(row => Math.Max(0, row.Debt)));
        }

        var recent = await db.Clients.AsNoTracking().OrderByDescending(client => client.Id).Take(5).ToListAsync(ct);
        return View(new Dashboard(await db.Clients.CountAsync(ct), recent, Group(false), Group(true), first, last));
    }

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
