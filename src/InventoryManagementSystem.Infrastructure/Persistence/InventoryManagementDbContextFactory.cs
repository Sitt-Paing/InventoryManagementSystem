using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventoryManagementSystem.Infrastructure.Persistence;

// Migration generation needs no running API, authentication, email worker, or database.
public class InventoryManagementDbContextFactory : IDesignTimeDbContextFactory<InventoryManagementDbContext>
{
    public InventoryManagementDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("IMS_DESIGN_CONNECTION")
            ?? "Server=.;Database=InventoryManagementDesign;Integrated Security=True;TrustServerCertificate=True";
        return new InventoryManagementDbContext(new DbContextOptionsBuilder<InventoryManagementDbContext>()
            .UseSqlServer(connection).Options);
    }
}
