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
    public string? ApprovedBy { get;private set; }
    public DateTime? ApprovedOn { get;private set; }
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

    public void Approve(string approvedBy, DateTime now)
    {
        if(string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException("Approved by cannot be null or empty.", nameof(approvedBy));
        }
        if (Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot approve a cancelled purchase order.");
        }
        if(ApprovalStatus != PurchaseOrderApprovalStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted purchase orders can be approved.");
        }
        ApprovalStatus = PurchaseOrderApprovalStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedOn = now;
    }
}
