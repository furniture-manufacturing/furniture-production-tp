using Furniture.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключение PostgreSQL: строка берётся из конфигурации (appsettings.Development.json)
builder.Services.AddDbContext<FurnitureDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FurnitureDb")));

// MVC
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
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Стартовая страница — список заказов
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Orders}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
