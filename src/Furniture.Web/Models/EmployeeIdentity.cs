using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Furniture.Web.Models;

public static class EmployeeIdentity
{
    public static int EmployeeId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static ClaimsPrincipal Principal(this Employee employee)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new(ClaimTypes.Name, employee.FullName)
        };
        claims.AddRange(employee.Roles.Distinct().Select(role => new Claim(ClaimTypes.Role, role.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
