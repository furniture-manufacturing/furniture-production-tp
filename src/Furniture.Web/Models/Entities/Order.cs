using Furniture.Web.Models.Enums;

namespace Furniture.Web.Models.Entities;

public class Order
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? Deadline { get; set; }

    // Клиент
    public int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    // Назначенный сотрудник
    public int? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }
}