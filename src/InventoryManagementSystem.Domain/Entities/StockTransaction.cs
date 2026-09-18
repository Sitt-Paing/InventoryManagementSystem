using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class StockTransaction : BaseAuditableEntity<long>
{

    public Guid ProductId { get; set; }

    public string UserId { get; set; } = null!;

    public int WarehouseId { get; set; }

    public int WarehouseLocationId { get; set; }

    public decimal Quantity { get; set; }

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public long? ReferenceNo { get; set; }

    public string? Note { get; set; }

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;
}
