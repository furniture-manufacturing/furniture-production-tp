namespace Furniture.Web.Models;

public class OrderStage
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public StageType Type { get; set; }
    public StageStatus Status { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Result { get; set; }
    public List<EmployeeAssignment> Assignments { get; set; } = new();
}
