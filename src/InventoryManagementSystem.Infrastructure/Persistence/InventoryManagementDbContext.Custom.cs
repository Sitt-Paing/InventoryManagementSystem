using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public partial class InventoryManagementDbContext : IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public int? CurrentCompanyId => _currentUserService?.CompanyId;
    public bool IsSuperAdminUser => _currentUserService == null || string.IsNullOrEmpty(_currentUserService.UserId) || _currentUserService.IsSuperAdmin;

    public InventoryManagementDbContext(
        DbContextOptions<InventoryManagementDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserName ?? _currentUserService?.UserId ?? "System";
        var currentCompanyId = CurrentCompanyId;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is IMustHaveCompany companyEntity)
            {
                if (!companyEntity.CompanyId.HasValue && currentCompanyId.HasValue)
                {
                    companyEntity.CompanyId = currentCompanyId.Value;
                }
            }
            if (entry.Entity is BaseAuditableEntity<long> auditableLong)
            {
                ApplyAuditValues(entry, auditableLong, currentUserId);
            }
            else if (entry.Entity is BaseAuditableEntity<string> auditableString)
            {
                ApplyAuditValues(entry, auditableString, currentUserId);
            }
            else if (entry.Entity is BaseAuditableEntity<Guid> auditableGuid)
            {
                ApplyAuditValues(entry, auditableGuid, currentUserId);
            }
            else if (entry.Entity is BaseAuditableEntity<int> auditableInt)
            {
                ApplyAuditValues(entry, auditableInt, currentUserId);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyAuditValues<TId>(
        EntityEntry entry,
        BaseAuditableEntity<TId> entity,
        string currentUserId)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entity.CreatedOn = DateTime.Now;
                entity.CreatedBy = currentUserId;
                break;

            case EntityState.Modified:
                entity.UpdatedOn = DateTime.Now;
                entity.UpdatedBy = currentUserId;
                break;

            case EntityState.Deleted:
                entry.State = EntityState.Modified;
                entity.DeletedOn = DateTime.Now;
                entity.DeletedBy = currentUserId;
                break;
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Multi-tenant Global Query Filters
        modelBuilder.Entity<Warehouse>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<WarehouseLocation>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<UomCategory>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<UnitOfMeasure>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<ProductUomConversion>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<GoodReceiptItem>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<Category>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<GoodReceipt>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<StockTransaction>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
        modelBuilder.Entity<WarehouseStocks>().HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Id)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrDefault(v)
                );

            entity.HasIndex(e => e.Sku, "IX_Products_Sku")
                .IsUnique()
                .HasFilter("[DeletedOn] IS NULL AND [SKU] IS NOT NULL");

            entity.HasIndex(x => x.Barcode, "IX_Products_Barcode")
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL AND [DeletedOn] IS NULL");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.Property(e => e.ProductId)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrEmpty(v)
                );
        });

        modelBuilder.Entity<ProductUomConversion>(entity =>
        {
            entity.Property(e => e.ProductId)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrEmpty(v)
                );
        });

        modelBuilder.Entity<WarehouseStocks>(entity =>
        {
            entity.Property(e => e.ProductId)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrEmpty(v)
                );
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
        });
    }

    private static Guid ParseGuidOrDefault(string v)
    {
        return Guid.Parse(v);
    }

    private static Guid ParseGuidOrEmpty(string v)
    {
        return Guid.Parse(v);
    }
}
