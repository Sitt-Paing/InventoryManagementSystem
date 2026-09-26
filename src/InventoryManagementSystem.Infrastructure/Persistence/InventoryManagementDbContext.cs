using System;
using System.Collections.Generic;
using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public partial class InventoryManagementDbContext : DbContext
{
    public InventoryManagementDbContext(DbContextOptions<InventoryManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductUomConversion> ProductUomConversions { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }

    public virtual DbSet<UomCategory> UomCategories { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseLocation> WarehouseLocations { get; set; }

    public virtual DbSet<WarehouseStocks> WarehouseStocks { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<GoodReceipt> GoodReceipts { get; set; }

    public virtual DbSet<GoodReceiptItem> GoodReceiptItems { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
            entity.HasOne(d => d.Company)
                .WithMany(p => p.AspNetUsers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AspNetUsers_Company");
            entity.HasIndex(e => e.CompanyId, "IX_AspNetUsers_CompanyId");
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Barcode, "IX_Products_Barcode")
                .IsUnique()
                .HasFilter("([Barcode] IS NOT NULL AND [DeletedOn] IS NULL)");

            entity.HasIndex(e => e.Sku, "IX_Products_Sku").IsUnique();

            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.ReorderLevel).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReorderQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.Property(e => e.Status).HasDefaultValue(true, "DF__Products__Status__1AD3FDA4");
            entity.Property(e => e.Tax).HasColumnType("decimal(18, 2)");
            entity.Ignore(e => e.Unit);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BaseUom).WithMany()
                .HasForeignKey(d => d.BaseUomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_BaseUom");

            entity.HasOne(d => d.PurchaseUom).WithMany()
                .HasForeignKey(d => d.PurchaseUomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_PurchaseUom");

            entity.HasOne(d => d.SaleUom).WithMany()
                .HasForeignKey(d => d.SaleUomId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Products_SaleUom");
        });

        modelBuilder.Entity<ProductUomConversion>(entity =>
        {
            entity.ToTable("ProductUomConversion");

            entity.Property(e => e.Barcode).HasMaxLength(50);
            entity.Property(e => e.ConversionFactor).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.FromUom).WithMany(p => p.ProductUomConversionFromUoms)
                .HasForeignKey(d => d.FromUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductUomConversion_UnitOfMeasure");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductUomConversions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductUomConversion_Products");

            entity.HasOne(d => d.ToUom).WithMany(p => p.ProductUomConversionToUoms)
                .HasForeignKey(d => d.ToUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductUomConversion_UnitOfMeasure1");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.ProductId).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TransactionDate).HasColumnType("datetime");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Product).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransactions_Products");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Supplier");

            entity.HasIndex(e => e.SupplierCode, "UQ_Supplier_SupplierCode").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.PaymentTerms).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.SupplierCode).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<UnitOfMeasure>(entity =>
        {
            entity.ToTable("UnitOfMeasure");

            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Symbol).HasMaxLength(20);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Category).WithMany(p => p.UnitOfMeasures)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UnitOfMeasure_UOM_Category");
        });

        modelBuilder.Entity<UomCategory>(entity =>
        {
            entity.ToTable("UOM_Category");

            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.Capacity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.WarehouseCode).HasMaxLength(50);
        });

        modelBuilder.Entity<WarehouseLocation>(entity =>
        {
            entity.ToTable("Warehouse_Locations");

            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Bin).HasMaxLength(50);
            entity.Property(e => e.Capacity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.LocationCode).HasMaxLength(50);
            entity.Property(e => e.Rack).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.Zone).HasMaxLength(50);

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseLocations)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_Locations_Warehouses");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("PurchaseOrders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchaseOrderNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasOne(d => d.Supplier)
             .WithMany()
             .HasForeignKey(d => d.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Warehouse)
                  .WithMany()
                  .HasForeignKey(d => d.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.ToTable("PurchaseOrderItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.PurchaseOrder)
                  .WithMany(p => p.Items)
                  .HasForeignKey(d => d.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Uom)
                  .WithMany()
                  .HasForeignKey(d => d.UomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodReceipt>(entity =>
        {
            entity.ToTable("GoodReceipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceiptNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReceiptDate).HasColumnType("datetime");
            entity.Property(e => e.ReceivedBy).HasMaxLength(100);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Supplier)
                  .WithMany()
                  .HasForeignKey(d => d.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Warehouse)
                  .WithMany()
                  .HasForeignKey(d => d.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.PurchaseOrder)
                  .WithMany()
                  .HasForeignKey(d => d.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Items)
                  .WithOne(p => p.GoodReceipt)
                  .HasForeignKey(p => p.GoodReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GoodReceiptItem>(entity =>
        {
            entity.ToTable("GoodReceiptItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReceivedQuantity).HasColumnType("decimal(18, 4)");

            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");

            entity.HasOne(d => d.GoodReceipt)
                  .WithMany(p => p.Items)
                  .HasForeignKey(d => d.GoodReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.PurchaseOrderItem)
                  .WithMany()
                  .HasForeignKey(d => d.PurchaseOrderItemId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Uom)
                  .WithMany()
                  .HasForeignKey(d => d.UomId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseStocks>(entity =>
        {
            entity.ToTable("WarehouseStocks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 4)");

            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");

            entity.HasIndex(e => new { e.ProductId, e.WarehouseId }, "UQ_WarehouseStocks_Product_Warehouse")
                  .IsUnique();

            entity.HasOne(d => d.Product)
                  .WithMany()
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_WarehouseStocks_Products");

            entity.HasOne(d => d.Warehouse)
                  .WithMany()
                  .HasForeignKey(d => d.WarehouseId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_WarehouseStocks_Warehouses");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(50).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");

        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
