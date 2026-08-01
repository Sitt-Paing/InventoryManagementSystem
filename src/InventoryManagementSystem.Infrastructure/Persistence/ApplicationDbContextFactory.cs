using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../InventoryManagementSystem.Api");
        var appsettingsPath = Path.Combine(basePath, "appsettings.json");
        var appsettingsDevPath = Path.Combine(basePath, "appsettings.Development.json");

        var builder = new ConfigurationBuilder();
        if (File.Exists(appsettingsPath))
        {
            builder.AddJsonFile(appsettingsPath, optional: true);
        }
        if (File.Exists(appsettingsDevPath))
        {
            builder.AddJsonFile(appsettingsDevPath, optional: true);
        }

        var configuration = builder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=InventoryManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
