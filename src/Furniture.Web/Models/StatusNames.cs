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
