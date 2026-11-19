namespace Furniture.Web.Models;

public class Order
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int ResponsibleAdministratorId { get; set; }
    public Employee ResponsibleAdministrator { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    // Адрес хранится в заказе и не зависит от адреса клиента
    public string ExecutionAddress { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CustomerWishes { get; set; }
    public decimal? AgreedCost { get; set; }
    public DateOnly PlannedCompletionDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    // null у обычного заказа; у подзаказа — родитель и причина
    public int? ParentOrderId { get; set; }
    public Order? ParentOrder { get; set; }
    public SubOrderReason? SubOrderReason { get; set; }

    public List<OrderStage> Stages { get; set; } = new();
}
