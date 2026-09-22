namespace InventoryManagementSystem.Application.Process.StockTransactions.DTOs;

public class ProductWarehouseStockDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
