using System.Data;
using Furniture.Web.Data;
using Furniture.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Services;

public sealed class OrderService(FurnitureDbContext db)
{
    public async Task<OrderCreationResult> CreateAsync(
        int clientId,
        string address,
        string? wishes,
        DateOnly plannedDate,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        if (!await db.Clients.AnyAsync(c => c.Id == clientId, cancellationToken))
            return new(null, OrderCreationError.ClientNotFound);

        var administratorId = await db.Employees
            .Where(e => e.Login == DemoData.AdministratorLogin && e.IsActive
                && e.Roles.Contains(EmployeeRole.Administrator))
            .Select(e => (int?)e.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (administratorId is null)
            return new(null, OrderCreationError.AdministratorUnavailable);

        // Проверка занятости и назначение выполняются под одной блокировкой.
        const long assignmentLock = 535101491;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({assignmentLock})", cancellationToken);

        var measurerId = await db.Employees
            .Where(e => e.IsActive && e.Roles.Contains(EmployeeRole.Measurer))
            .Where(e => !db.EmployeeAssignments.Any(a => a.EmployeeId == e.Id
                && (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.Accepted)))
            .Select(e => new
            {
                e.Id,
                LastAssignedAt = db.EmployeeAssignments
                    .Where(a => a.EmployeeId == e.Id)
                    .Max(a => (DateTime?)a.AssignedAt)
            })
            .OrderBy(e => e.LastAssignedAt.HasValue)
            .ThenBy(e => e.LastAssignedAt)
            .ThenBy(e => e.Id)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var stage = new OrderStage
        {
            Type = StageType.MeasurementAndApproval,
            Status = StageStatus.WaitingForAssignee
        };
        var order = new Order
        {
            Number = $"МП-{Guid.NewGuid():N}",
            ClientId = clientId,
            ResponsibleAdministratorId = administratorId.Value,
            CreatedAt = now,
            ExecutionAddress = address.Trim(),
            CustomerWishes = wishes?.Trim(),
            PlannedCompletionDate = plannedDate,
            Status = OrderStatus.New,
            Stages = [stage]
        };
        db.Orders.Add(order);

        if (measurerId is int employeeId)
        {
            stage.Assignments.Add(new EmployeeAssignment
            {
                EmployeeId = employeeId,
                RoleOnStage = EmployeeRole.Measurer,
                AssignedAt = now,
                Status = AssignmentStatus.Assigned
            });
            stage.Status = StageStatus.Assigned;
            order.Status = OrderStatus.MeasurementAndApproval;
            db.ChangeHistory.AddRange(
                new ChangeHistory
                {
                    Order = order,
                    ChangedAt = now,
                    Action = "Назначение замерщика",
                    NewValue = employeeId.ToString()
                },
                new ChangeHistory
                {
                    Order = order,
                    ChangedAt = now,
                    Action = "Изменение статуса заказа",
                    OldValue = nameof(OrderStatus.New),
                    NewValue = nameof(OrderStatus.MeasurementAndApproval)
                });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(order.Id, null);
    }
}

public readonly record struct OrderCreationResult(int? OrderId, OrderCreationError? Error);

public enum OrderCreationError
{
    ClientNotFound,
    AdministratorUnavailable
}
