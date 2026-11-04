using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public sealed class LowStockNotification : BaseAuditableEntity<Guid>, IMustHaveCompany
{
    public int? CompanyId { get; set; }

    public Guid ProductId { get; set; }

    public decimal CurrentQuantity { get; set; }

    public decimal ReorderLevel { get; set; }
}
