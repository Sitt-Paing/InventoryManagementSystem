using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Report;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<StockBalance> StockBalances { get; }
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
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<GoodReceipt> GoodReceipts { get; }
    DbSet<GoodReceiptItem> GoodReceiptItems { get; }
    DbSet<Company> Companies { get; }
    DbSet<Department> Departments { get; }
    DbSet<EmailOutbox> EmailOutboxes { get; }
    DbSet<PurchaseReturn> PurchaseReturns { get; }
    DbSet<PurchaseReturnItem> PurchaseReturnItems { get; }
    DbSet<StockIssue> StockIssues { get; }
    DbSet<StockIssueItem> StockIssueItems { get; }

    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.Serializable);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task LockPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

    Task<long> NextPurchaseOrderNumberAsync(CancellationToken cancellationToken = default);
}
