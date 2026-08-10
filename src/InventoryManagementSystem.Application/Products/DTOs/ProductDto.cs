namespace InventoryManagementSystem.Application.Products.DTOs;

public record ProductDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public long CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
