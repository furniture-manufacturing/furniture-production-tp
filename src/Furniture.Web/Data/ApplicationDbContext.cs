using Furniture.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Order> Orders => Set<Order>();
}