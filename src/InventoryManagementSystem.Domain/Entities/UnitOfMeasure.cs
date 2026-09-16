using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class UnitOfMeasure
{
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Symbol { get; set; }

    public int DecimalPlaces { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

    [JsonIgnore]
    public virtual UomCategory Category { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<ProductUomConversion> ProductUomConversionFromUoms { get; set; } = new List<ProductUomConversion>();

    [JsonIgnore]
    public virtual ICollection<ProductUomConversion> ProductUomConversionToUoms { get; set; } = new List<ProductUomConversion>();
}
