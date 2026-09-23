using InventoryManagementSystem.Domain.Common;
using System;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseOrder : BaseAuditableEntity<Guid>
{
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public bool Status { get; set; }
    public decimal TotalAmount { get; set; }

    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }
    [JsonIgnore]
    public virtual Warehouse? Warehouse { get; set; }

    [JsonIgnore]
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}