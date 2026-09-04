using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Warehouse: BaseAuditableEntity<int>
{
    public string WarehouseCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal? Capacity { get; set; }

    public bool Status { get; set; }

    [JsonIgnore]
    public virtual ICollection<WarehouseLocation> WarehouseLocations { get; set; } = new List<WarehouseLocation>();
}
