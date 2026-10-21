namespace InventoryManagementSystem.Application.SupplierPortal.DTOs;

public class SupplierDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierAddress { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? PurchaseOrderNo { get; set; }
    public string? Note { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SupplierDocumentItemDto> Items { get; set; } = new();
}

public class SupplierDocumentItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public string UomName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? WarehouseName { get; set; }
    public string? ReceiptNo { get; set; }
    public string? PurchaseOrderNo { get; set; }
}
