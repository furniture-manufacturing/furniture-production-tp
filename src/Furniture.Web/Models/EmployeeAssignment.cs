namespace Furniture.Web.Models;

public class EmployeeAssignment
{
    public int Id { get; set; }
    public int OrderStageId { get; set; }
    public OrderStage OrderStage { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public EmployeeRole RoleOnStage { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AssignmentStatus Status { get; set; }
}
