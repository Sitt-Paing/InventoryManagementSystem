namespace InventoryManagementSystem.Application.Master.Products.DTOs;

public record ProductDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public long CategoryId { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public long BaseUomId { get; set; }
    public string? BaseUomCode { get; set; }
    public string? BaseUomName { get; set; }
    public long? PurchaseUomId { get; set; }
    public string? PurchaseUomCode { get; set; }
    public string? PurchaseUomName { get; set; }
    public long? SaleUomId { get; set; }
    public string? SaleUomCode { get; set; }
    public string? SaleUomName { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public decimal Tax { get; set; }
    public bool Status { get; set; } = true;
    public string? Description { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
