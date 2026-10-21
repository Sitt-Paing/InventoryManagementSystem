namespace InventoryManagementSystem.Application.SupplierPortal.DTOs;

public class SupplierDocumentSummaryDto
{
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SupplierDashboardDto
{
    public SupplierDocumentSummaryDto PurchaseOrders { get; set; } = new();
    public SupplierDocumentSummaryDto GoodsReceipts { get; set; } = new();
    public SupplierDocumentSummaryDto PurchaseReturns { get; set; } = new();
    public decimal NetReceivedAmount { get; set; }
}
