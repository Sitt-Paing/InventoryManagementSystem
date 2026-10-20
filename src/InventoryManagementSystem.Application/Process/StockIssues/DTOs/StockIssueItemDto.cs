namespace InventoryManagementSystem.Application.Process.StockIssues.DTOs;

public class StockIssueItemDto
{
    public long Id { get; set; }
    public Guid StockIssueId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public long UomId { get; set; }
    public string UomName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
