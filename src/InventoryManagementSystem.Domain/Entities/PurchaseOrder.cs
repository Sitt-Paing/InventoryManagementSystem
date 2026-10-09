using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Enums;
using System;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseOrder : BaseAuditableEntity<Guid>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? CreateRequestHash { get; set; }
    public string? CancellationReason { get; set; }
    public PurchaseOrderApprovalStatus ApprovalStatus { get; private set; } = PurchaseOrderApprovalStatus.Draft;

    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }
    [JsonIgnore]
    public virtual Warehouse? Warehouse { get; set; }

    [JsonIgnore]
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    public void SubmitForApproval()
    {
        if(Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot submit a cancelled purchase order for approval.");
        }
        if (ApprovalStatus != PurchaseOrderApprovalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be submitted for approval.");
        }
        if (Items == null || Items.Count == 0)
        {
            throw new InvalidOperationException("Cannot submit a purchase order without items.");
        }
        ApprovalStatus = PurchaseOrderApprovalStatus.Submitted;
    }
}
