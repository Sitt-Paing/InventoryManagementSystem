using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public partial class InventoryManagementDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public InventoryManagementDbContext(
        DbContextOptions<InventoryManagementDbContext> options,
        ICurrentUserService? currentUserService = null)
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

    private static void ApplyAuditValues<TId>(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, BaseAuditableEntity<TId> entity, string currentUserId)
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

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

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
            entity.Property(e => e.Id)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrDefault(v)
                );
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.HasIndex(e => e.Sku, "IX_Products_Sku").IsUnique();
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.HasIndex(x => x.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(256);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(250);
            entity.Property(e => e.ProductId)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseGuidOrEmpty(v)
                );
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

        OnModelCreatingPartial(modelBuilder);
    }

    private static Guid ParseGuidOrDefault(string v)
    {
        Guid g;
        return Guid.TryParse(v, out g) ? g : Guid.NewGuid();
    }

    private static Guid ParseGuidOrEmpty(string v)
    {
        Guid g;
        return Guid.TryParse(v, out g) ? g : Guid.Empty;
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
