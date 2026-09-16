using System.Collections.Generic;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class UnitOfMeasure : BaseAuditableEntity<long>
{
    public long CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Symbol { get; set; }

    public int DecimalPlaces { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual UomCategory Category { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<ProductUomConversion> ProductUomConversionFromUoms { get; set; } = new List<ProductUomConversion>();

    [JsonIgnore]
    public virtual ICollection<ProductUomConversion> ProductUomConversionToUoms { get; set; } = new List<ProductUomConversion>();
}
