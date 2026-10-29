using InventoryManagementSystem.Domain.Report;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public partial class InventoryManagementDbContext
{
    public virtual DbSet<StockBalance> StockBalances { get; set; }

    private void ConfigureReports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("VI_StockBalance", "dbo");
            entity.HasQueryFilter(e => CurrentCompanyId.HasValue && e.CompanyId == CurrentCompanyId);
            entity.Property(e => e.ProductId)
                .HasConversion(v => v.ToString(), v => ParseGuidOrDefault(v));
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
        });
    }
}
