using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<UomCategory> UomCategories { get; }
    DbSet<UnitOfMeasure> UnitOfMeasures { get; }
    DbSet<ProductUomConversion> ProductUomConversions { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<WarehouseStocks> WarehouseStocks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
