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

    public string? Brand { get; set; }

    public string? Unit { get; set; }

    public string? Barcode { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    [JsonIgnore]
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal UnitPrice
    {
        get => SellingPrice;
        set => SellingPrice = value;
    }

    public int CurrentStock { get; set; }

    public int ReorderLevel { get; set; }

    public int ReorderQuantity { get; set; }

    public decimal Tax { get; set; }

    public bool Status { get; set; } = true;

    public string? Description { get; set; }

    [JsonIgnore]
    public virtual Category Category { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
