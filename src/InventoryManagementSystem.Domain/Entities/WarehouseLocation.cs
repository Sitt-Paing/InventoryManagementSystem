using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class WarehouseLocation: BaseAuditableEntity<int>
{
    public int WarehouseId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string? Zone { get; set; }

    public string? Rack { get; set; }

    public string? Bin { get; set; }

    public string? Barcode { get; set; }

    public decimal? Capacity { get; set; }

    public bool Status { get; set; }

    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}
