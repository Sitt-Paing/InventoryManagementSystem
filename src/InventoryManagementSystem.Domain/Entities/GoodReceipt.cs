using InventoryManagementSystem.Domain.Common;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class GoodReceipt: BaseAuditableEntity<Guid>
{
    public string ReceiptNo { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public bool Status { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Note { get; set; }

    [JsonIgnore]
    public virtual Warehouse? Warehouse { get; set; }

    [JsonIgnore]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }

    [JsonIgnore]
    public virtual ICollection<GoodReceiptItem> Items { get; set; } = new List<GoodReceiptItem>();
}