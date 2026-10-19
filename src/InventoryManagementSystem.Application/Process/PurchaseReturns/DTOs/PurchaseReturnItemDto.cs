namespace InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;

public class PurchaseReturnItemDto
{
    public long Id { get; set; }
    public Guid PurchaseReturnId { get; set; }
    public long GoodReceiptItemId { get; set; }
    public Guid GoodReceiptId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long UomId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal ReturnableQuantity { get; set; }
    public decimal ReturnQuantity { get; set; }
}
