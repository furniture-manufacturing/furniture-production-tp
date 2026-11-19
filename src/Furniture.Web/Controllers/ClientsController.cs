using Furniture.Web.Data;
using Furniture.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Furniture.Web.Controllers;

public sealed class ClientsController(FurnitureDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    public IActionResult Create() => environment.IsDevelopment() ? View(new ClientForm()) : NotFound();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientForm form, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (!ModelState.IsValid)
            return View(form);

        var client = new Client
        {
            FullName = form.FullName.Trim(),
            Phone = form.Phone.Trim(),
            Email = form.Email?.Trim(),
            DefaultAddress = form.DefaultAddress.Trim()
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(OrdersController.Create), "Orders", new { clientId = client.Id });
    }
}
