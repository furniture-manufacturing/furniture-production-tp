using Furniture.Web.Data;
using Furniture.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FurnitureDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Укажите ConnectionStrings:FurnitureDb в настройках приложения.");

builder.Services.AddDbContext<FurnitureDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<OrderService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Миграции и демоданные применяются автоматически только в Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FurnitureDbContext>();
    db.Database.Migrate();
    DemoData.Seed(db);
}

// Обработка ошибок
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Стартовая страница — список заказов
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Orders}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
