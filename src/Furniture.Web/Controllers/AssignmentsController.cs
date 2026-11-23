using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Controllers;

public sealed class AssignmentsController(FurnitureDbContext db, OrderService orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var employeeId = User.EmployeeId();
        return View(await db.EmployeeAssignments.AsNoTracking().Include(assignment => assignment.OrderStage)
            .ThenInclude(stage => stage.Order).ThenInclude(order => order.Client)
            .Where(assignment => assignment.EmployeeId == employeeId &&
                (assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted))
            .OrderBy(assignment => assignment.AssignedAt).ThenBy(assignment => assignment.Id).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var employeeId = User.EmployeeId();
        var assignment = await db.EmployeeAssignments.AsNoTracking()
            .Where(assignment => assignment.Id == id && assignment.EmployeeId == employeeId &&
                (assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted))
            .Select(assignment => new { assignment.OrderStageId, assignment.OrderStage.OrderId }).SingleOrDefaultAsync(ct);
        if (assignment is null)
            return NotFound();
        var order = await orders.Cards.AsNoTracking().SingleAsync(order => order.Id == assignment.OrderId, ct);
        var stage = order.Stages.Single(stage => stage.Id == assignment.OrderStageId);
        var current = stage.Assignments.FirstOrDefault(entry => entry.Id == id && OrderService.IsOpen(entry));
        return current is null ? NotFound() : View(current);
    }

    [HttpPost]
    public Task<IActionResult> Accept(int id, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.Accept(order, stage, employee), false, ct);

    [HttpPost]
    public Task<IActionResult> Measurement(int id, MeasurementForm form, bool complete, bool newVisit, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.SaveMeasurement(order, stage, employee, form, complete, newVisit), complete, ct);

    [HttpPost]
    public Task<IActionResult> Item(int id, int? itemId, ItemForm form, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.SaveItem(order, itemId, form, employee, stage), false, ct);

    [HttpPost]
    public Task<IActionResult> Element(int id, int itemId, string? name, int quantity, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.AddElement(order, stage, employee, itemId, name, quantity), false, ct);

    [HttpPost]
    public Task<IActionResult> Progress(int id, int elementId, int? quantity, string? problem, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.UpdateElement(order, stage, employee, elementId,
            quantity ?? throw new WorkflowException("Укажите количество."), problem), false, ct);

    [HttpPost]
    public Task<IActionResult> Work(int id, DateOnly? plannedDate, string? result, bool complete, CancellationToken ct) =>
        Change(id, (order, stage, employee) => orders.SaveWork(order, stage, employee, plannedDate, result, complete), complete, ct);

    private async Task<IActionResult> Change(int id, Action<Order, int, int> change, bool finish, CancellationToken ct)
    {
        var employeeId = User.EmployeeId();
        var assignment = await db.EmployeeAssignments.AsNoTracking().Where(assignment =>
                assignment.Id == id && assignment.EmployeeId == employeeId &&
                (assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted))
            .Select(assignment => new { assignment.OrderStageId, assignment.OrderStage.OrderId }).SingleOrDefaultAsync(ct);
        if (assignment is null)
            return NotFound();

        try
        {
            OrderService.Require(ModelState.IsValid, "Проверьте заполнение полей и формат введённых данных.");
            await orders.ChangeAsync(assignment.OrderId, order =>
            {
                OrderService.Require(order.Stages.Any(stage => stage.Id == assignment.OrderStageId &&
                    stage.Assignments.Any(entry => entry.Id == id && OrderService.IsOpen(entry))), "Назначение уже закрыто.");
                change(order, assignment.OrderStageId, employeeId);
            }, ct);
            TempData["Message"] = "Изменения сохранены.";
            return finish ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }
        catch (WorkflowException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
