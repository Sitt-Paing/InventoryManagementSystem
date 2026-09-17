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

    public decimal CurrentStock { get; set; }

    public decimal ReorderLevel { get; set; }

    public decimal ReorderQuantity { get; set; }

    public decimal Tax { get; set; }

    public long BaseUomId { get; set; }

    public long? PurchaseUomId { get; set; }

    public long? SaleUomId { get; set; }

    public bool Status { get; set; } = true;

    public string? Description { get; set; }

    [JsonIgnore]
    public virtual Category Category { get; set; } = null!;

    [JsonIgnore]
    public virtual UnitOfMeasure BaseUom { get; set; } = null!;

    [JsonIgnore]
    public virtual UnitOfMeasure? PurchaseUom { get; set; }

    [JsonIgnore]
    public virtual UnitOfMeasure? SaleUom { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    [JsonIgnore]
    public virtual ICollection<ProductUomConversion> ProductUomConversions { get; set; } = new List<ProductUomConversion>();
}

