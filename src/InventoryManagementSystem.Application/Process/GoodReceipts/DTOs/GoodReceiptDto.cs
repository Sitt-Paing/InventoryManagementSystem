using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;

public class GoodReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierCode { get; set; }
    public DateTime ReceiptDate { get; set; }
    public bool Status { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int TotalItems => Items?.Count ?? 0;
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    public List<GoodReceiptItemDto> Items { get; set; } = new();
}
