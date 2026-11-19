namespace Furniture.Web.Models;

public class ChangeHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    // null, если изменение сделала система
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Comment { get; set; }
}
