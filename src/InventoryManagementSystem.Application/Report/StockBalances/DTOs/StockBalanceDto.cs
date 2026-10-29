namespace InventoryManagementSystem.Application.Report.StockBalances.DTOs;

public record StockBalanceDto
{
    public long WarehouseStockId { get; set; }
    public int? CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public string? Sku { get; set; }
    public string ProductName { get; set; } = null!;
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string UomName { get; set; } = null!;
    public string? UomSymbol { get; set; }
    public DateTime? StockCreatedOn { get; set; }
    public string? StockCreatedBy { get; set; }
    public DateTime? StockUpdatedOn { get; set; }
    public string? StockUpdatedBy { get; set; }
    public DateTime? StockDeletedOn { get; set; }
    public string? StockDeletedBy { get; set; }
}
