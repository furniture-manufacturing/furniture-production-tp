namespace Furniture.Web.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    // Роли — набор значений, а не классы-наследники (раздел 10 спецификации)
    public List<EmployeeRole> Roles { get; set; } = new();
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
