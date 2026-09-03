using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public partial class InventoryManagementDbContext : IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public InventoryManagementDbContext(
        DbContextOptions<InventoryManagementDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserName ?? _currentUserService?.UserId ?? "System";

        foreach (var entry in ChangeTracker.Entries())
        {
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
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyAuditValues<TId>(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
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
    }

    private static Guid ParseGuidOrDefault(string v)
    {
        return Guid.TryParse(v, out Guid g) ? g : Guid.NewGuid();
    }

    private static Guid ParseGuidOrEmpty(string v)
    {
        return Guid.TryParse(v, out Guid g) ? g : Guid.Empty;
    }
}
