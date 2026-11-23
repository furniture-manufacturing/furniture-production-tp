using Furniture.Web.Data;
using Furniture.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Services;

public sealed partial class OrderService(FurnitureDbContext db)
{
    public IQueryable<Order> Cards => db.Orders
        .Include(order => order.Client)
        .Include(order => order.ResponsibleAdministrator)
        .Include(order => order.ParentOrder)
        .Include(order => order.Items).ThenInclude(item => item.Elements)
        .Include(order => order.Measurements).ThenInclude(measurement => measurement.Measurer)
        .Include(order => order.Payments).ThenInclude(payment => payment.Administrator)
        .Include(order => order.Stages).ThenInclude(stage => stage.Assignments).ThenInclude(assignment => assignment.Employee)
        .AsSplitQuery();

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({535101491L})", ct);
        var result = await action();
        await transaction.CommitAsync(ct);
        return result;
    }

    public Task<int> CreateAsync(OrderForm form, int administratorId, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            var parent = form.ParentOrderId is int parentId
                ? await db.Orders.SingleOrDefaultAsync(order => order.Id == parentId, ct) : null;
            Require(form.ParentOrderId is null || parent is not null && parent.Status != OrderStatus.Cancelled,
                "Исходный заказ недоступен.");
            Require(parent is null || form.SubOrderReason.HasValue, "Укажите причину подзаказа.");
            var clientId = parent?.ClientId ?? form.ClientId;
            Require(clientId.HasValue && await db.Clients.AnyAsync(client => client.Id == clientId, ct),
                "Выбранный клиент не найден.");

            var order = new Order
            {
                ClientId = clientId!.Value,
                ResponsibleAdministratorId = administratorId,
                CreatedAt = DateTime.UtcNow,
                ExecutionAddress = form.ExecutionAddress.Trim(),
                CustomerWishes = form.CustomerWishes?.Trim(),
                PlannedCompletionDate = form.PlannedCompletionDate!.Value,
                ParentOrder = parent,
                SubOrderReason = parent is null ? null : form.SubOrderReason,
                Number = $"МП-{Guid.NewGuid():N}",
                Stages = Enum.GetValues<StageType>().Select(type => new OrderStage
                {
                    Type = type,
                    QueuedAt = type == StageType.MeasurementAndApproval ? DateTime.UtcNow : null,
                    Status = type == StageType.MeasurementAndApproval
                        ? StageStatus.WaitingForAssignee : StageStatus.WaitingPreviousStage
                }).ToList()
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            order.Number = $"МП-{order.Id:D6}";
            History(order, administratorId, "Создание заказа");
            await FillQueueAsync(ct);
            return order.Id;
        }, ct);

    public Task ChangeAsync(int orderId, Action<Order> change, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            var order = await Cards.SingleOrDefaultAsync(order => order.Id == orderId, ct);
            Require(order is not null, "Заказ не найден.");
            change(order!);
            await FillQueueAsync(ct);
            return true;
        }, ct);

    public async Task FillQueueAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        var queue = await db.OrderStages.Include(stage => stage.Order)
            .Where(stage => stage.Status == StageStatus.WaitingForAssignee)
            .OrderBy(stage => stage.QueuedAt ?? stage.Order.CreatedAt).ThenBy(stage => stage.Id).ToListAsync(ct);
        if (queue.Count == 0)
            return;

        var employees = await db.Employees.Where(employee => employee.IsActive).ToListAsync(ct);
        var availability = await db.EmployeeAssignments.GroupBy(assignment => assignment.EmployeeId)
            .Select(group => new
            {
                EmployeeId = group.Key,
                LastAssigned = group.Max(assignment => assignment.AssignedAt),
                Busy = group.Any(assignment =>
                    assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted)
            }).ToDictionaryAsync(employee => employee.EmployeeId, ct);
        var candidates = employees.Where(employee => availability.GetValueOrDefault(employee.Id)?.Busy != true)
            .OrderBy(employee => availability.GetValueOrDefault(employee.Id)?.LastAssigned)
            .ThenBy(employee => employee.Id).ToList();

        foreach (var stage in queue)
        {
            var role = stage.Type.WorkerRole();
            var employee = candidates.FirstOrDefault(employee => employee.Roles.Contains(role));
            if (employee is null)
                continue;
            Assign(stage.Order, stage, employee, null);
            candidates.Remove(employee);
        }
        await db.SaveChangesAsync(ct);
    }

    public static bool IsOpen(EmployeeAssignment assignment) =>
        assignment.Status is AssignmentStatus.Assigned or AssignmentStatus.Accepted;

    public static bool IsClosed(Order order) =>
        order.Status is OrderStatus.Completed or OrderStatus.Cancelled;

    public static bool IsFinished(OrderStage stage) =>
        stage.Status is StageStatus.Completed or StageStatus.Skipped;

    public static void Require(bool condition, string message)
    {
        if (!condition)
            throw new WorkflowException(message);
    }

    private void History(Order order, int? actor, string action, string? oldValue = null, string? newValue = null,
        string? comment = null) =>
        db.ChangeHistory.Add(new ChangeHistory
        {
            Order = order, EmployeeId = actor, ChangedAt = DateTime.UtcNow,
            Action = action, OldValue = oldValue, NewValue = newValue, Comment = comment
        });

    private void SetStatus(Order order, OrderStatus status, int? actor)
    {
        if (order.Status == status)
            return;
        History(order, actor, "Изменение статуса заказа", order.Status.Ru(), status.Ru());
        order.Status = status;
        order.CompletedAt = status == OrderStatus.Completed ? DateTime.UtcNow : null;
    }

    private void Assign(Order order, OrderStage stage, Employee employee, int? actor, string? replaced = null)
    {
        stage.Assignments.Add(new EmployeeAssignment
        {
            Employee = employee, RoleOnStage = stage.Type.WorkerRole(), AssignedAt = DateTime.UtcNow,
            Status = AssignmentStatus.Assigned
        });
        stage.Status = stage.Assignments.Any(assignment => assignment.Status == AssignmentStatus.Accepted)
            ? StageStatus.InProgress : StageStatus.Assigned;
        History(order, actor, replaced is null ? "Назначение исполнителя" : "Замена исполнителя",
            replaced, employee.FullName, stage.Type.Ru());
        if (stage.Type == StageType.MeasurementAndApproval)
            SetStatus(order, OrderStatus.MeasurementAndApproval, actor);
    }

    private static OrderStage Stage(Order order, StageType type)
    {
        var stage = order.Stages.SingleOrDefault(stage => stage.Type == type);
        if (stage is not null)
            return stage;
        stage = new OrderStage { Order = order, Type = type, Status = StageStatus.WaitingPreviousStage };
        order.Stages.Add(stage);
        return stage;
    }
}

public sealed class WorkflowException(string message) : Exception(message);
