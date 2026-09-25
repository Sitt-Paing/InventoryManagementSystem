using System;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;

public class GoodReceiptItemDto
{
    public long Id { get; set; }
    public Guid GoodReceiptId { get; set; }
    public long PurchaseOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public long UomId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
}
