using System.ComponentModel.DataAnnotations;

namespace Furniture.Web.Models;

public sealed class EmployeeForm
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = "";

    [Required, StringLength(30)]
    public string Phone { get; set; } = "";

    [Required, RegularExpression("[a-zA-Z0-9_.-]{3,40}")]
    public string Login { get; set; } = "";

    [StringLength(128, MinimumLength = 12)]
    public string? Password { get; set; }

    public EmployeeRole[] Roles { get; set; } = [];
}
