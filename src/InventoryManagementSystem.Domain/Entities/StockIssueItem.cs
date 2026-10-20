using InventoryManagementSystem.Domain.Common;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class StockIssueItem : BaseAuditableEntity<long>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public Guid StockIssueId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }

    [JsonIgnore]
    public virtual StockIssue? StockIssue { get; set; }

    [JsonIgnore]
    public virtual Product? Product { get; set; }
}
