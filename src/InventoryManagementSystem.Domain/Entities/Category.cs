using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Category : BaseAuditableEntity<long>, IMustHaveCompany
{
    public int? CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
