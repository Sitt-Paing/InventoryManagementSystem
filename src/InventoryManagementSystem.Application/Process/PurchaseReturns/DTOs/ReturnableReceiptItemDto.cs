namespace InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;

public class ReturnableReceiptItemDto
{
    public long GoodReceiptItemId { get; set; }
    public Guid GoodReceiptId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal ReturnableQuantity { get; set; }
}
