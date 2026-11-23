using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public sealed class ClientsController(FurnitureDbContext db, OrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var query = db.Clients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var value = "%" + q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(client => EF.Functions.ILike(client.FullName, value) ||
                EF.Functions.ILike(client.Phone, value));
        }
        ViewBag.Clients = await query.OrderBy(client => client.FullName).ThenBy(client => client.Id).ToListAsync(ct);
        return View(new ClientForm());
    }

    [HttpGet]
    public IActionResult Create(bool forOrder = true)
    {
        ViewBag.ForOrder = forOrder;
        return View(new ClientForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ClientForm form, bool forOrder, CancellationToken ct)
    {
        ViewBag.ForOrder = forOrder;
        if (!ModelState.IsValid)
            return View(form);
        var client = new Client();
        Apply(client, form);
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return forOrder
            ? RedirectToAction(nameof(OrdersController.Create), "Orders", new { clientId = client.Id })
            : RedirectToAction(nameof(Details), new { id = client.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(client => client.Id == id, ct);
        if (client is null)
            return NotFound();
        ViewBag.Client = client;
        ViewBag.Orders = await db.Orders.AsNoTracking().Where(order => order.ClientId == id)
            .OrderByDescending(order => order.CreatedAt).ToListAsync(ct);
        return View(new ClientForm
        {
            FullName = client.FullName, Phone = client.Phone, Email = client.Email, DefaultAddress = client.DefaultAddress
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClientForm form, CancellationToken ct)
    {
        var client = await db.Clients.FindAsync([id], ct);
        if (client is null)
            return NotFound();
        if (ModelState.IsValid)
        {
            Apply(client, form);
            await db.SaveChangesAsync(ct);
            TempData["Message"] = "Данные клиента обновлены.";
        }
        else
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage));
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await orders.InTransactionAsync(async () =>
            {
                var client = await db.Clients.FindAsync([id], ct);
                OrderService.Require(client is not null, "Клиент не найден.");
                OrderService.Require(!await db.Orders.AnyAsync(order => order.ClientId == id, ct),
                    "Удаление доступно для клиентов без заказов.");
                db.Clients.Remove(client!);
                await db.SaveChangesAsync(ct);
                return true;
            }, ct);
            TempData["Message"] = "Клиент удалён.";
            return RedirectToAction(nameof(Index));
        }
        catch (WorkflowException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private static void Apply(Client client, ClientForm form)
    {
        client.FullName = form.FullName.Trim();
        client.Phone = form.Phone.Trim();
        client.DefaultAddress = form.DefaultAddress.Trim();
        client.Email = form.Email?.Trim();
    }
}
