using Furniture.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Furniture.Web.Data;

public class FurnitureDbContext : DbContext
{
    public FurnitureDbContext(DbContextOptions<FurnitureDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStage> OrderStages => Set<OrderStage>();
    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();
    public DbSet<ChangeHistory> ChangeHistory => Set<ChangeHistory>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ProductElement> ProductElements => Set<ProductElement>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.Property(c => c.FullName).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(30);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.DefaultAddress).HasMaxLength(500);
        });

        b.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.Login).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(200);
        });

        b.Entity<Order>(e =>
        {
            e.HasIndex(o => o.Number).IsUnique();
            e.Property(o => o.Number).HasMaxLength(50);
            e.Property(o => o.ExecutionAddress).HasMaxLength(500);
            e.Property(o => o.AgreedCost).HasPrecision(12, 2);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(40);
            e.Property(o => o.SubOrderReason).HasConversion<string>().HasMaxLength(40);

            // Client 1 — 0..* Order
            e.HasOne(o => o.Client).WithMany().HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            // Employee 1 — 0..* Order (ответственный администратор)
            e.HasOne(o => o.ResponsibleAdministrator).WithMany()
                .HasForeignKey(o => o.ResponsibleAdministratorId)
                .OnDelete(DeleteBehavior.Restrict);
            // Самосвязь: подзаказ — тот же Order с ParentOrderId
            e.HasOne(o => o.ParentOrder).WithMany().HasForeignKey(o => o.ParentOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            // Order 1 — 1..* OrderStage
            e.HasMany(o => o.Stages).WithOne(s => s.Order).HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<OrderStage>(e =>
        {
            e.Property(s => s.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(40);
            // OrderStage 1 — 0..* EmployeeAssignment
            e.HasMany(s => s.Assignments).WithOne(a => a.OrderStage).HasForeignKey(a => a.OrderStageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EmployeeAssignment>(e =>
        {
            e.Property(a => a.RoleOnStage).HasConversion<string>().HasMaxLength(40);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(40);
            // Employee 1 — 0..* EmployeeAssignment
            e.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<OrderItem>().Property(i => i.AgreedUnitPrice).HasPrecision(12, 2);
        b.Entity<Measurement>().HasOne(m => m.Measurer).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().HasOne(p => p.Administrator).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Payment>().Property(p => p.Amount).HasPrecision(12, 2);
        b.Entity<OrderStage>().HasIndex(s => new { s.OrderId, s.Type }).IsUnique();

        b.Entity<ChangeHistory>(e =>
        {
            e.HasOne(h => h.Order).WithMany().HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            // Employee 0..1 — 0..* ChangeHistory
            e.HasOne(h => h.Employee).WithMany().HasForeignKey(h => h.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
