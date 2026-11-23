namespace Furniture.Web.Models;

// Русские названия для отображения (по разделу 5 спецификации)
public static class StatusNames
{
    public static string Ru(this OrderStatus s) => s switch
    {
        OrderStatus.New => "Новый",
        OrderStatus.MeasurementAndApproval => "Замер и согласование",
        OrderStatus.InProduction => "В производстве",
        OrderStatus.ReadyForDelivery => "Готов к доставке",
        OrderStatus.Delivery => "Доставка",
        OrderStatus.Assembly => "Сборка",
        OrderStatus.AwaitingPayment => "Ожидает оплаты",
        OrderStatus.Completed => "Выполнен",
        OrderStatus.Cancelled => "Отменён",
        _ => s.ToString()
    };

    public static string Ru(this StageStatus s) => s switch
    {
        StageStatus.WaitingPreviousStage => "Ожидает предыдущий этап",
        StageStatus.WaitingForAssignee => "Ожидает исполнителя",
        StageStatus.Assigned => "Назначен исполнитель",
        StageStatus.InProgress => "В работе",
        StageStatus.Completed => "Завершён",
        StageStatus.Skipped => "Пропущен",
        _ => s.ToString()
    };

    public static string Ru(this StageType t) => t switch
    {
        StageType.MeasurementAndApproval => "Замер и согласование",
        StageType.Production => "Производство",
        StageType.Delivery => "Доставка",
        StageType.Assembly => "Сборка",
        _ => t.ToString()
    };
}

public static class WorkflowNames
{
    public static EmployeeRole WorkerRole(this StageType type) => type switch
    {
        StageType.MeasurementAndApproval => EmployeeRole.Measurer,
        StageType.Production => EmployeeRole.ProductionWorker,
        StageType.Delivery => EmployeeRole.Courier,
        StageType.Assembly => EmployeeRole.Assembler,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string Ru(this EmployeeRole role) => role switch
    {
        EmployeeRole.Administrator => "Администратор",
        EmployeeRole.Measurer => "Замерщик",
        EmployeeRole.ProductionWorker => "Работник производства",
        EmployeeRole.Courier => "Доставщик",
        EmployeeRole.Assembler => "Сборщик",
        _ => role.ToString()
    };

    public static string Ru(this AssignmentStatus status) => status switch
    {
        AssignmentStatus.Assigned => "Назначено",
        AssignmentStatus.Accepted => "В работе",
        AssignmentStatus.Completed => "Завершено",
        AssignmentStatus.Cancelled => "Снято",
        _ => status.ToString()
    };

    public static string Ru(this SubOrderReason reason) => reason switch
    {
        SubOrderReason.Addition => "Дополнение",
        SubOrderReason.MissingPart => "Нехватка детали",
        SubOrderReason.Defect => "Брак",
        SubOrderReason.Rework => "Доработка",
        _ => reason.ToString()
    };

    public static string Ru(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Наличные",
        PaymentMethod.Transfer => "Перевод",
        PaymentMethod.Card => "Карта",
        _ => method.ToString()
    };

    public static string Ru(this PaymentType type) => type == PaymentType.Receipt ? "Поступление" : "Возврат";
}
