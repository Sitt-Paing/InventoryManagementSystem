using System;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;

public class PurchaseOrderItemDto
{
    public long Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public long UomId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
}
