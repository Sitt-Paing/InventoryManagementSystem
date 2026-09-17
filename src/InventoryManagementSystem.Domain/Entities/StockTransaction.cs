using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Domain.Entities;

public partial class StockTransaction
{
    public long Id { get; set; }

    public string ProductId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public int WarehouseId { get; set; }

    public int WarehouseLocationId { get; set; }

    public decimal Quantity { get; set; }

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public long? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    public virtual Product Product { get; set; } = null!;
}
