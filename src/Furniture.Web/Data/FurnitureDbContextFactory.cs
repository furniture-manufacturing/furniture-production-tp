using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Furniture.Web.Data;

public sealed class FurnitureDbContextFactory : IDesignTimeDbContextFactory<FurnitureDbContext>
{
    public FurnitureDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var options = new DbContextOptionsBuilder<FurnitureDbContext>()
            .UseNpgsql(settings.GetConnectionString("FurnitureDb"))
            .Options;
        return new FurnitureDbContext(options);
    }
}
