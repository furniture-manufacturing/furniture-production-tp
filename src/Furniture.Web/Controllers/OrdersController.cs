using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

public sealed class OrdersController(
    FurnitureDbContext db, OrderService orders, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var list = await db.Orders.AsNoTracking()
            .Include(o => o.Client)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? clientId, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var clients = await LoadClientsAsync(cancellationToken);
        var client = clients.FirstOrDefault(c => c.Id == clientId);
        return View(new OrderForm
        {
            ClientId = client?.Id,
            ExecutionAddress = client?.DefaultAddress ?? string.Empty,
            Clients = clients
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderForm form, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (ModelState.IsValid
            && form.ClientId is int clientId
            && form.PlannedCompletionDate is DateOnly plannedDate)
        {
            var result = await orders.CreateAsync(
                clientId, form.ExecutionAddress, form.CustomerWishes, plannedDate, cancellationToken);

            if (result.OrderId is int orderId)
                return RedirectToAction(nameof(Details), new { id = orderId });

            if (result.Error == OrderCreationError.ClientNotFound)
                ModelState.AddModelError(nameof(form.ClientId), "Выбранный клиент не найден.");
            else
                ModelState.AddModelError(string.Empty, "Для создания заказа нужен активный демо-администратор.");
        }

        form.Clients = await LoadClientsAsync(cancellationToken);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Client)
            .Include(o => o.ResponsibleAdministrator)
            .Include(o => o.Stages).ThenInclude(s => s.Assignments).ThenInclude(a => a.Employee)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    private Task<List<ClientChoice>> LoadClientsAsync(CancellationToken cancellationToken) =>
        db.Clients.AsNoTracking()
            .OrderBy(c => c.FullName)
            .ThenBy(c => c.Id)
            .Select(c => new ClientChoice(c.Id, c.FullName, c.Phone, c.DefaultAddress))
            .ToListAsync(cancellationToken);
}
