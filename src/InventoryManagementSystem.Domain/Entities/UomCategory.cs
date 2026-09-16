using System.Collections.Generic;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class UomCategory : BaseAuditableEntity<long>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public virtual ICollection<UnitOfMeasure> UnitOfMeasures { get; set; } = new List<UnitOfMeasure>();
}
