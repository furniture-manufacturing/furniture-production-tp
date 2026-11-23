using Furniture.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Furniture.Web.Data;

public static class DemoData
{
    public static void Seed(FurnitureDbContext db, IPasswordHasher<Employee> passwords)
    {
        EnsureAdministrator(db, passwords, "AdminDemo2026");
        var workers = new (string Login, EmployeeRole Role, string Name)[]
        {
            ("measurer1", EmployeeRole.Measurer, "Замерщиков Пётр Сергеевич"),
            ("production1", EmployeeRole.ProductionWorker, "Производственный мастер"),
            ("courier1", EmployeeRole.Courier, "Доставщик"),
            ("assembler1", EmployeeRole.Assembler, "Сборщик")
        };
        foreach (var worker in workers)
            EnsureEmployee(db, passwords, worker.Login, worker.Role, worker.Name, "WorkerDemo2026");

        if (!db.Clients.Any())
        {
            db.Clients.Add(new Client
            {
                FullName = "Иванов Иван Иванович",
                Phone = "+7 900 111-22-33",
                Email = "ivanov@example.com",
                DefaultAddress = "г. Владимир, ул. Горького, д. 1, кв. 10"
            });
        }
        db.SaveChanges();
    }

    public static void EnsureAdministrator(FurnitureDbContext db, IPasswordHasher<Employee> passwords, string password)
    {
        if (password.Length is < 12 or > 128)
            throw new InvalidOperationException("Пароль администратора должен содержать от 12 до 128 символов.");
        EnsureEmployee(db, passwords, "admin", EmployeeRole.Administrator, "Администратор", password);
        db.SaveChanges();
    }

    private static void EnsureEmployee(FurnitureDbContext db, IPasswordHasher<Employee> passwords,
        string login, EmployeeRole role, string name, string password)
    {
        var employee = db.Employees.SingleOrDefault(e => e.Login == login);
        if (employee is null)
        {
            employee = new Employee { Login = login, FullName = name, Roles = [role] };
            db.Employees.Add(employee);
        }
        else if (employee.PasswordHash != "not-set")
        {
            return;
        }

        employee.PasswordHash = passwords.HashPassword(employee, password);
    }
}
