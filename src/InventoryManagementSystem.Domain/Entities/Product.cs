using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Product : BaseAuditableEntity<Guid>
{
    public string Name { get; set; } = null!;

    public string? Sku { get; set; }

    public long CategoryId { get; set; }

    public decimal UnitPrice { get; set; }

    public int CurrentStock { get; set; }

    public int ReorderLevel { get; set; }

    [JsonIgnore]
    public virtual Category Category { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
