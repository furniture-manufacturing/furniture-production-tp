using Furniture.Web.Models.Enums;

namespace Furniture.Web.Models.Entities;

public class Employee
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public EmployeePosition Position { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
}