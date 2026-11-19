using Furniture.Web.Models;

namespace Furniture.Web.Data;

// Воспроизводимые демоданные: один администратор, один активный замерщик, один клиент.
// Загружаются при первом запуске в Development, если таблица сотрудников пуста.
public static class DemoData
{
    public static void Seed(FurnitureDbContext db)
    {
        if (db.Employees.Any()) return;

        db.Employees.AddRange(
            new Employee
            {
                FullName = "Администратор Демо",
                Phone = "+7 900 000-00-01",
                Roles = new() { EmployeeRole.Administrator },
                Login = "admin",
                PasswordHash = "not-set", // вход и пароли — вне этапа 3
                IsActive = true
            },
            new Employee
            {
                FullName = "Замерщиков Пётр Сергеевич",
                Phone = "+7 900 000-00-02",
                Roles = new() { EmployeeRole.Measurer },
                Login = "measurer1",
                PasswordHash = "not-set",
                IsActive = true
            });

        db.Clients.Add(new Client
        {
            FullName = "Иванов Иван Иванович",
            Phone = "+7 900 111-22-33",
            Email = "ivanov@example.com",
            DefaultAddress = "г. Владимир, ул. Горького, д. 1, кв. 10"
        });

        db.SaveChanges();
    }
}
