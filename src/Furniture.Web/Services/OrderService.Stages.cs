using Furniture.Web.Models;

namespace Furniture.Web.Services;

public sealed partial class OrderService
{
    public void Accept(Order order, int stageId, int employeeId)
    {
        var stage = WorkerStage(order, stageId, employeeId, AssignmentStatus.Assigned);
        var assignment = stage.Assignments.Single(assignment => assignment.EmployeeId == employeeId &&
            assignment.Status == AssignmentStatus.Assigned);
        var now = DateTime.UtcNow;
        assignment.Status = AssignmentStatus.Accepted;
        assignment.AcceptedAt = now;
        stage.Status = StageStatus.InProgress;
        stage.StartedAt ??= now;
        if (stage.Type == StageType.Delivery)
            SetStatus(order, OrderStatus.Delivery, employeeId);
        History(order, employeeId, "Назначение принято", comment: stage.Type.Ru());
    }

    public void SaveMeasurement(Order order, int stageId, int employeeId, MeasurementForm form, bool complete, bool newVisit)
    {
        var stage = WorkerStage(order, stageId, employeeId, AssignmentStatus.Accepted);
        Require(stage.Type == StageType.MeasurementAndApproval, "Этот этап не связан с замером.");
        var measurement = newVisit ? null : order.Measurements.Where(measurement =>
            measurement.MeasurerId == employeeId && measurement.Status == MeasurementStatus.Scheduled)
            .MaxBy(measurement => measurement.Id);
        if (measurement is null)
        {
            measurement = new Measurement { MeasurerId = employeeId };
            order.Measurements.Add(measurement);
        }

        Require(!complete || form.ActualDate.HasValue && !string.IsNullOrWhiteSpace(form.RoomDimensions),
            "Для завершения замера заполните фактическую дату и размеры помещения.");
        measurement.PlannedDate = form.PlannedDate;
        measurement.ActualDate = form.ActualDate;
        measurement.RoomDimensions = form.RoomDimensions?.Trim();
        measurement.PlacementFeatures = form.PlacementFeatures?.Trim();
        measurement.CustomerWishes = form.CustomerWishes?.Trim();
        measurement.Comment = form.Comment?.Trim();
        if (!complete)
            return;

        measurement.Status = MeasurementStatus.Completed;
        History(order, employeeId, "Замер завершён");
        if (order.Measurements.Any(visit => visit.MeasurerId == employeeId && visit.Status == MeasurementStatus.Scheduled))
            return;

        var assignment = stage.Assignments.Single(assignment => assignment.EmployeeId == employeeId &&
            assignment.Status == AssignmentStatus.Accepted);
        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTime.UtcNow;
        if (stage.Assignments.All(assignment => !IsOpen(assignment)))
            stage.Status = StageStatus.InProgress;
    }

    public void SaveItem(Order order, int? itemId, ItemForm form, int actor, int? stageId)
    {
        if (stageId is int id)
        {
            var stage = WorkerStage(order, id, actor, AssignmentStatus.Accepted);
            Require(stage.Type == StageType.MeasurementAndApproval, "Изделия добавляются на этапе замера.");
        }
        Require(order.Status is OrderStatus.New or OrderStatus.MeasurementAndApproval,
            "Состав заказа можно изменить до передачи в производство.");
        var item = order.Items.SingleOrDefault(item => item.Id == itemId);
        if (itemId.HasValue)
            Require(item is not null, "Изделие не найдено.");
        if (item is null)
        {
            item = new OrderItem();
            order.Items.Add(item);
        }
        item.FurnitureType = form.FurnitureType.Trim();
        item.Name = form.Name.Trim();
        item.Quantity = form.Quantity;
        item.Width = form.Width;
        item.Height = form.Height;
        item.Depth = form.Depth;
        item.Material = form.Material.Trim();
        item.Color = form.Color.Trim();
        item.AdditionalCharacteristics = form.AdditionalCharacteristics?.Trim();
        if (stageId is null)
            item.AgreedUnitPrice = form.AgreedUnitPrice;
    }

    public void RemoveItem(Order order, int itemId)
    {
        Require(order.Status is OrderStatus.New or OrderStatus.MeasurementAndApproval,
            "Состав заказа можно изменить до передачи в производство.");
        var item = order.Items.SingleOrDefault(item => item.Id == itemId);
        Require(item is not null, "Изделие не найдено.");
        db.OrderItems.Remove(item!);
    }

    public void Agree(Order order, decimal cost, int actor)
    {
        Require(order.Status == OrderStatus.MeasurementAndApproval,
            "Согласование доступно на этапе замера.");
        var stage = Stage(order, StageType.MeasurementAndApproval);
        Require(order.Measurements.Any(measurement => measurement.Status == MeasurementStatus.Completed) &&
            stage.Assignments.All(assignment => !IsOpen(assignment)),
            "Сначала завершите замер и работу назначенных замерщиков.");
        Require(order.Items.Count > 0, "Добавьте изделия до согласования.");
        SetCost(order, cost, actor);
        Finish(stage, actor);
        OpenNext(order, stage.Type, actor);
    }

    public void Skip(Order order, StageType type, string? reason, int actor)
    {
        Require(order.ParentOrderId.HasValue && !IsClosed(order) && !string.IsNullOrWhiteSpace(reason),
            "Пропуск этапа доступен только для подзаказа. Укажите причину.");
        var stage = Stage(order, type);
        Require(!IsFinished(stage) && stage.Status != StageStatus.WaitingPreviousStage && stage.StartedAt is null,
            "Можно пропустить текущий этап, по которому ещё не начались работы.");
        if (type == StageType.MeasurementAndApproval)
            Require(order.AgreedCost.HasValue && order.Items.Count > 0, "Добавьте изделия и укажите стоимость подзаказа.");
        stage.Status = StageStatus.Skipped;
        stage.Result = reason!.Trim();
        stage.CompletedAt = DateTime.UtcNow;
        StopAssignments(stage);
        History(order, actor, "Этап пропущен", newValue: type.Ru(), comment: stage.Result);
        OpenNext(order, type, actor);
    }

    public void AddElement(Order order, int stageId, int actor, int itemId, string? name, int quantity)
    {
        var stage = WorkerStage(order, stageId, actor, AssignmentStatus.Accepted);
        Require(CanDefineElements(order, stage.Type), "Список элементов формируется на первом применимом этапе работ.");
        var item = order.Items.SingleOrDefault(item => item.Id == itemId);
        Require(item is not null && !string.IsNullOrWhiteSpace(name) && name.Length <= 200 && quantity is > 0 and <= 1000000,
            "Укажите изделие, название и положительное количество элементов.");
        item!.Elements.Add(new ProductElement { Name = name!.Trim(), RequiredQuantity = quantity });
    }

    public void UpdateElement(Order order, int stageId, int actor, int elementId, int quantity, string? problem)
    {
        var stage = WorkerStage(order, stageId, actor, AssignmentStatus.Accepted);
        var element = order.Items.SelectMany(item => item.Elements).SingleOrDefault(element => element.Id == elementId);
        Require(element is not null && quantity >= 0 && quantity <= element.RequiredQuantity,
            "Количество должно быть от нуля до требуемого.");
        switch (stage.Type)
        {
            case StageType.Production:
                element!.ProducedQuantity = quantity;
                break;
            case StageType.Delivery:
                Require(IsSkipped(order, StageType.Production) || quantity <= element!.ProducedQuantity,
                    "Сначала изготовьте необходимое количество.");
                element!.DeliveredQuantity = quantity;
                break;
            case StageType.Assembly:
                var available = !IsSkipped(order, StageType.Delivery) ? element!.DeliveredQuantity
                    : !IsSkipped(order, StageType.Production) ? element!.ProducedQuantity
                    : element!.RequiredQuantity;
                Require(quantity <= available, "Для сборки недостаточно готовых элементов.");
                element.InstalledQuantity = quantity;
                break;
            default:
                throw new WorkflowException("На замере параметры изделий указываются отдельно.");
        }
        if (!string.IsNullOrWhiteSpace(problem))
        {
            element.ProblemComment = problem.Trim();
            History(order, actor, "Проблема с элементом", newValue: element.Name,
                comment: $"{stage.Type.Ru()} · {element.ProblemComment}");
        }
    }

    public void SaveWork(Order order, int stageId, int actor, DateOnly? plannedDate, string? result, bool complete)
    {
        var stage = WorkerStage(order, stageId, actor, AssignmentStatus.Accepted);
        Require(stage.Type != StageType.MeasurementAndApproval, "Заполните результаты замера.");
        stage.PlannedDate = plannedDate;
        stage.Result = result?.Trim();
        if (!complete)
            return;

        var remaining = order.Items.SelectMany(item => item.Elements)
            .Where(element => (stage.Type switch
            {
                StageType.Production => element.ProducedQuantity,
                StageType.Delivery => element.DeliveredQuantity,
                _ => element.InstalledQuantity
            }) < element.RequiredQuantity).Select(element => element.Name).ToList();
        var missingItems = order.Items.Where(item => item.Elements.Count == 0).Select(item => item.Name).ToList();
        Require(order.Items.Count > 0 && missingItems.Count == 0,
            "Составьте список элементов для каждого изделия. " + string.Join(", ", missingItems));
        Require(remaining.Count == 0, "Осталось выполнить работы по элементам: " + string.Join(", ", remaining));

        Finish(stage, actor);
        OpenNext(order, stage.Type, actor);
    }

    public static bool CanDefineElements(Order order, StageType type) =>
        type == StageType.Production ||
        type == StageType.Delivery && IsSkipped(order, StageType.Production) ||
        type == StageType.Assembly && IsSkipped(order, StageType.Production) && IsSkipped(order, StageType.Delivery);

    private static bool IsSkipped(Order order, StageType type) =>
        order.Stages.Any(stage => stage.Type == type && stage.Status == StageStatus.Skipped);

    private void Finish(OrderStage stage, int actor)
    {
        var now = DateTime.UtcNow;
        stage.CompletedAt = now;
        stage.Status = StageStatus.Completed;
        foreach (var assignment in stage.Assignments.Where(IsOpen))
        {
            assignment.Status = assignment.Status == AssignmentStatus.Accepted
                ? AssignmentStatus.Completed : AssignmentStatus.Cancelled;
            assignment.CompletedAt = now;
        }
        History(stage.Order, actor, "Этап завершён", newValue: stage.Type.Ru());
    }

    private void OpenNext(Order order, StageType completed, int actor)
    {
        if (completed == StageType.Assembly)
        {
            UpdateSettlement(order, actor);
            return;
        }

        var next = Stage(order, completed + 1);
        next.Status = StageStatus.WaitingForAssignee;
        next.QueuedAt = DateTime.UtcNow;
        SetStatus(order, completed switch
        {
            StageType.MeasurementAndApproval => OrderStatus.InProduction,
            StageType.Production => OrderStatus.ReadyForDelivery,
            _ => OrderStatus.Assembly
        }, actor);
    }

    private static OrderStage WorkerStage(Order order, int stageId, int employeeId, AssignmentStatus status)
    {
        var stage = order.Stages.SingleOrDefault(stage => stage.Id == stageId);
        Require(order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled) &&
            stage is not null && !IsFinished(stage) && stage.Assignments.Any(assignment =>
                assignment.EmployeeId == employeeId && assignment.Status == status &&
                assignment.Employee.IsActive && assignment.Employee.Roles.Contains(assignment.RoleOnStage)),
            "Назначение недоступно для этого действия.");
        return stage!;
    }
}
