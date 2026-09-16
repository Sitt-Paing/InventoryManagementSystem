using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Domain.Entities;

public partial class ProductUomConversion
{
    public long Id { get; set; }

    public string ProductId { get; set; } = null!;

    public long FromUomId { get; set; }

    public long ToUomId { get; set; }

    public decimal ConversionFactor { get; set; }

    public string? Barcode { get; set; }

    public bool IsDefaultPurchase { get; set; }

    public bool IsDefaultSale { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    public virtual UnitOfMeasure FromUom { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual UnitOfMeasure ToUom { get; set; } = null!;
}
