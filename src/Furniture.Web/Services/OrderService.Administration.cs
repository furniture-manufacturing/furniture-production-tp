using Furniture.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Services;

public sealed partial class OrderService
{
    public void Edit(Order order, OrderForm form, int actor)
    {
        Require(!IsClosed(order), "Завершённый или отменённый заказ нельзя редактировать.");
        order.ExecutionAddress = form.ExecutionAddress.Trim();
        order.CustomerWishes = form.CustomerWishes?.Trim();
        order.PlannedCompletionDate = form.PlannedCompletionDate!.Value;
        History(order, actor, "Данные заказа обновлены");
    }

    public void SetCost(Order order, decimal cost, int actor)
    {
        Require(!IsClosed(order) && cost is >= 0 and <= 9999999999.99m && decimal.Round(cost, 2) == cost,
            "Укажите стоимость от 0 до 9 999 999 999,99 ₽ с точностью до копеек.");
        if (order.AgreedCost != cost)
        {
            History(order, actor, "Согласованная стоимость изменена", order.AgreedCost?.ToString("N2"), cost.ToString("N2"));
            order.AgreedCost = cost;
        }
        UpdateSettlement(order, actor);
    }

    public void AddPayment(Order order, decimal amount, PaymentType type, PaymentMethod method, string? comment, int actor)
    {
        Require(amount is > 0 and <= 9999999999.99m && decimal.Round(amount, 2) == amount &&
            Enum.IsDefined(type) && Enum.IsDefined(method), "Проверьте сумму и реквизиты платежа.");
        Require(type == PaymentType.Refund ? amount <= order.NetPaid : order.Status != OrderStatus.Cancelled,
            "Поступление для отменённого заказа или возврат сверх полученной суммы недоступны.");
        order.Payments.Add(new Payment
        {
            Amount = amount, Type = type, Method = method, Date = DateTime.UtcNow,
            AdministratorId = actor, Comment = comment?.Trim()
        });
        History(order, actor, type.Ru(), newValue: amount.ToString("N2"), comment: comment?.Trim());
        UpdateSettlement(order, actor);
    }

    public void Cancel(Order order, string? reason, int actor)
    {
        Require(!IsClosed(order) && !string.IsNullOrWhiteSpace(reason), "Укажите причину отмены незавершённого заказа.");
        foreach (var stage in order.Stages.Where(stage => !IsFinished(stage)))
        {
            stage.Status = StageStatus.Skipped;
            stage.Result = reason!.Trim();
            stage.CompletedAt = DateTime.UtcNow;
            StopAssignments(stage);
        }
        History(order, actor, "Отмена заказа", comment: reason!.Trim());
        SetStatus(order, OrderStatus.Cancelled, actor);
    }

    public void AssignManually(Order order, int stageId, int employeeId, int? replaceId, int actor)
    {
        var stage = order.Stages.SingleOrDefault(stage => stage.Id == stageId);
        Require(!IsClosed(order) && stage is not null && !IsFinished(stage) && stage.Status != StageStatus.WaitingPreviousStage,
            "Назначение доступно для открытого этапа.");
        var employee = db.Employees.Find(employeeId);
        Require(employee is { IsActive: true } && employee.Roles.Contains(stage!.Type.WorkerRole()) &&
            !db.EmployeeAssignments.Any(assignment => assignment.EmployeeId == employeeId &&
                (assignment.Status == AssignmentStatus.Accepted || assignment.Status == AssignmentStatus.Assigned)),
            "Выберите свободного действующего сотрудника с ролью этого этапа.");

        var previous = stage!.Assignments.SingleOrDefault(assignment => assignment.Id == replaceId && IsOpen(assignment));
        Require(!replaceId.HasValue || previous is not null, "Заменяемое назначение уже закрыто.");
        if (previous is not null)
            StopAssignment(previous);
        Assign(order, stage, employee!, actor, previous?.Employee.FullName);
    }

    public void RevokeAssignments(Employee employee, int actor)
    {
        var active = db.EmployeeAssignments.Include(assignment => assignment.OrderStage)
            .ThenInclude(stage => stage.Order)
            .Include(assignment => assignment.OrderStage).ThenInclude(stage => stage.Assignments)
            .Where(assignment => assignment.EmployeeId == employee.Id &&
                (assignment.Status == AssignmentStatus.Assigned || assignment.Status == AssignmentStatus.Accepted))
            .ToList();

        foreach (var assignment in active.Where(assignment =>
            !employee.IsActive || !employee.Roles.Contains(assignment.RoleOnStage)))
        {
            StopAssignment(assignment);
            var stage = assignment.OrderStage;
            var order = stage.Order;
            History(order, actor, "Снятие назначения", oldValue: employee.FullName, comment: stage.Type.Ru());
            if (stage.Assignments.Any(assignment => assignment.Status == AssignmentStatus.Accepted))
                stage.Status = StageStatus.InProgress;
            else if (stage.Assignments.Any(IsOpen))
                stage.Status = StageStatus.Assigned;
            else
            {
                stage.Status = StageStatus.WaitingForAssignee;
                stage.QueuedAt = DateTime.UtcNow;
                if (stage.Type == StageType.MeasurementAndApproval)
                    SetStatus(order, OrderStatus.New, actor);
                if (stage.Type == StageType.Delivery)
                    SetStatus(order, OrderStatus.ReadyForDelivery, actor);
            }
        }
    }

    private void UpdateSettlement(Order order, int actor)
    {
        if (order.Status != OrderStatus.Cancelled &&
            Enum.GetValues<StageType>().All(type => order.Stages.Any(stage => stage.Type == type && IsFinished(stage))))
            SetStatus(order, order.Balance is <= 0 ? OrderStatus.Completed : OrderStatus.AwaitingPayment, actor);
    }

    private static void StopAssignments(OrderStage stage)
    {
        foreach (var assignment in stage.Assignments.Where(IsOpen))
            StopAssignment(assignment);
    }

    private static void StopAssignment(EmployeeAssignment assignment)
    {
        assignment.Status = AssignmentStatus.Cancelled;
        assignment.CompletedAt = DateTime.UtcNow;
    }
}
