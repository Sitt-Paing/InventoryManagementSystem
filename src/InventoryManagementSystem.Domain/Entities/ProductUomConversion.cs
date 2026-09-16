using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class ProductUomConversion : BaseAuditableEntity<long>
{
    public Guid ProductId { get; set; }

    public long FromUomId { get; set; }

    public long ToUomId { get; set; }

    public decimal ConversionFactor { get; set; }

    public string? Barcode { get; set; }

    public bool IsDefaultPurchase { get; set; }

    public bool IsDefaultSale { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual UnitOfMeasure FromUom { get; set; } = null!;

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;

    [JsonIgnore]
    public virtual UnitOfMeasure ToUom { get; set; } = null!;
}
