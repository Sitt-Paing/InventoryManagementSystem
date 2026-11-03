using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Domain.Entities;

public class LowStockOutbox : BaseAuditableEntity<Guid>, IMustHaveCompany
{
    public int? CompanyId { get; set; }

    public Guid ProductId { get; set; }

    public decimal PreviousQuantity { get; set; }

    public decimal CurrentQuantity { get; set; }

    public decimal ReorderLevel { get; set; }

    public DateTime? PublishedOn { get; private set; }

    public void MarkPublished(DateTime now)
    {
        PublishedOn ??= now;
    }
}
