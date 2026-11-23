using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

[Authorize(Roles = nameof(EmployeeRole.Administrator))]
public sealed class OrdersController(FurnitureDbContext db, OrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, OrderStatus? status, int? clientId, int? employeeId,
        DateOnly? from, DateOnly? to, bool waiting, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Include(order => order.Client).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = "%" + q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var digits = new string(q.Where(char.IsAsciiDigit).ToArray());
            query = query.Where(order => EF.Functions.ILike(order.Number, search) ||
                EF.Functions.ILike(order.Client.FullName, search) || EF.Functions.ILike(order.Client.Phone, search) ||
                (digits.Length > 0 && order.Client.Phone.Replace(" ", "").Replace("-", "")
                    .Replace("(", "").Replace(")", "").Contains(digits)));
        }
        if (status.HasValue)
            query = query.Where(order => order.Status == status);
        if (clientId.HasValue)
            query = query.Where(order => order.ClientId == clientId);
        if (employeeId.HasValue)
            query = query.Where(order => order.Stages.Any(stage =>
                stage.Assignments.Any(assignment => assignment.EmployeeId == employeeId)));
        if (from.HasValue)
        {
            var start = WorkshopDates.Start(from.Value);
            query = query.Where(order => order.CreatedAt >= start);
        }
        if (to.HasValue && to < DateOnly.MaxValue)
        {
            var end = WorkshopDates.Start(to.Value.AddDays(1));
            query = query.Where(order => order.CreatedAt < end);
        }
        if (waiting)
            query = query.Where(order => order.Stages.Any(stage => stage.Status == StageStatus.WaitingForAssignee));

        ViewBag.Clients = await db.Clients.AsNoTracking().OrderBy(client => client.FullName).ToListAsync(ct);
        ViewBag.Employees = await db.Employees.AsNoTracking().OrderBy(employee => employee.FullName).ToListAsync(ct);
        return View(await query.OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? clientId, int? parentOrderId, CancellationToken ct)
    {
        var parent = parentOrderId.HasValue
            ? await db.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.Id == parentOrderId, ct) : null;
        if (parentOrderId.HasValue && parent is null)
            return NotFound();

        var form = await LoadChoices(new OrderForm
        {
            ClientId = parent?.ClientId ?? clientId,
            ParentOrderId = parent?.Id,
            ParentNumber = parent?.Number,
            ExecutionAddress = parent?.ExecutionAddress ?? "",
            PlannedCompletionDate = parent?.PlannedCompletionDate
        }, ct);
        return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderForm form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await orders.CreateAsync(form, User.EmployeeId(), ct);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (WorkflowException exception)
            {
                ModelState.AddModelError("", exception.Message);
            }
        }
        return View(await LoadChoices(form, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var order = await orders.Cards.AsNoTracking().SingleOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
            return NotFound();
        ViewBag.Children = await db.Orders.AsNoTracking().Where(child => child.ParentOrderId == id)
            .OrderBy(child => child.Id).ToListAsync(ct);
        ViewBag.History = await db.ChangeHistory.AsNoTracking().Include(entry => entry.Employee)
            .Where(entry => entry.OrderId == id).OrderByDescending(entry => entry.ChangedAt).ThenByDescending(entry => entry.Id).ToListAsync(ct);
        var busyIds = db.EmployeeAssignments.Where(assignment =>
            assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted)
            .Select(assignment => assignment.EmployeeId);
        ViewBag.Available = await db.Employees.AsNoTracking()
            .Where(employee => employee.IsActive && !busyIds.Contains(employee.Id))
            .OrderBy(employee => employee.FullName).ToListAsync(ct);
        return View(order);
    }

    [HttpPost]
    public Task<IActionResult> Edit(int id, OrderForm form, CancellationToken ct)
    {
        ModelState.Remove(nameof(OrderForm.ClientId));
        return Change(id, order => orders.Edit(order, form, User.EmployeeId()), ct);
    }

    [HttpPost]
    public Task<IActionResult> Item(int id, int? itemId, ItemForm form, CancellationToken ct) =>
        Change(id, order => orders.SaveItem(order, itemId, form, User.EmployeeId(), null), ct);

    [HttpPost]
    public Task<IActionResult> RemoveItem(int id, int itemId, CancellationToken ct) =>
        Change(id, order => orders.RemoveItem(order, itemId), ct);

    [HttpPost]
    public Task<IActionResult> Agree(int id, decimal? cost, CancellationToken ct) =>
        Change(id, order => orders.Agree(order, Cost(cost), User.EmployeeId()), ct);

    [HttpPost]
    public Task<IActionResult> Price(int id, decimal? cost, CancellationToken ct) =>
        Change(id, order => orders.SetCost(order, Cost(cost), User.EmployeeId()), ct);

    [HttpPost]
    public Task<IActionResult> Payment(int id, decimal amount, PaymentType type, PaymentMethod method, string? comment,
        CancellationToken ct) =>
        Change(id, order => orders.AddPayment(order, amount, type, method, comment, User.EmployeeId()), ct);

    [HttpPost]
    public Task<IActionResult> Cancel(int id, string? reason, CancellationToken ct) =>
        Change(id, order => orders.Cancel(order, reason, User.EmployeeId()), ct);

    [HttpPost]
    public Task<IActionResult> Assign(int id, int stageId, int employeeId, int? replaceId, CancellationToken ct) =>
        Change(id, order => orders.AssignManually(order, stageId, employeeId, replaceId, User.EmployeeId()), ct);

    [HttpPost]
    public Task<IActionResult> Skip(int id, StageType type, string? reason, CancellationToken ct) =>
        Change(id, order => orders.Skip(order, type, reason, User.EmployeeId()), ct);

    private async Task<IActionResult> Change(int id, Action<Order> change, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return InvalidFields(id);
        try
        {
            await orders.ChangeAsync(id, change, ct);
            TempData["Message"] = "Изменения сохранены.";
        }
        catch (WorkflowException exception)
        {
            TempData["Error"] = exception.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult InvalidFields(int id)
    {
        TempData["Error"] = "Проверьте значения полей и формат даты или суммы.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static decimal Cost(decimal? cost) => cost ?? throw new WorkflowException("Укажите стоимость заказа.");

    private async Task<OrderForm> LoadChoices(OrderForm form, CancellationToken ct)
    {
        form.Clients = await db.Clients.AsNoTracking().OrderBy(client => client.FullName)
            .Select(client => new ClientChoice(client.Id, client.FullName, client.Phone, client.DefaultAddress)).ToListAsync(ct);
        if (form.ParentOrderId.HasValue && form.ParentNumber is null)
            form.ParentNumber = await db.Orders.Where(order => order.Id == form.ParentOrderId)
                .Select(order => order.Number).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(form.ExecutionAddress))
            form.ExecutionAddress = form.Clients.FirstOrDefault(client => client.Id == form.ClientId)?.DefaultAddress ?? "";
        return form;
    }
}
