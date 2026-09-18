using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class WarehouseStocks : BaseAuditableEntity<long>
{
    public Guid ProductId { get; set; }
    public int WarehouseId { get; set; }
    public decimal Quantity { get; set; }

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;
    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}
