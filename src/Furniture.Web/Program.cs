using System.Globalization;
using System.Security.Claims;
using Furniture.Web.Data;
using Furniture.Web.Models;
using Furniture.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("FurnitureDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Укажите ConnectionStrings:FurnitureDb в настройках приложения.");

builder.Services.AddDbContext<FurnitureDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<FurnitureDbContext>();
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(
            e => e.Id == context.Principal!.EmployeeId() && e.IsActive, context.HttpContext.RequestAborted);
        if (employee is null)
        {
            context.RejectPrincipal();
            return;
        }

        var roles = context.Principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value);
        if (!roles.ToHashSet().SetEquals(employee.Roles.Select(role => role.ToString())))
            context.ReplacePrincipal(employee.Principal());
    };
});

var app = builder.Build();
var culture = CultureInfo.GetCultureInfo("ru-RU");
app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new(culture);
    options.SupportedCultures = [culture];
    options.SupportedUICultures = [culture];
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FurnitureDbContext>();
    db.Database.Migrate();
    DemoData.Seed(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>());
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
    var initialPassword = app.Configuration["InitialAdministratorPassword"];
    if (!string.IsNullOrWhiteSpace(initialPassword))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FurnitureDbContext>();
        DemoData.EnsureAdministrator(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>(), initialPassword);
    }
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
