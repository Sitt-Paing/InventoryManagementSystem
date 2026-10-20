using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class StockIssue : BaseAuditableEntity<Guid>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public int WarehouseId { get; set; }
    public int DepartmentId { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
    public StockIssueStatus Status { get; set; } = StockIssueStatus.Draft;
    public DateTime? PostedOn { get; set; }
    public string? PostedBy { get; set; }

    [JsonIgnore]
    public virtual Warehouse? Warehouse { get; set; }

    [JsonIgnore]
    public virtual Department? Department { get; set; }

    [JsonIgnore]
    public virtual ICollection<StockIssueItem> Items { get; set; } = new List<StockIssueItem>();
}
