using System;

namespace InventoryManagementSystem.Application.Master.ProductUomConversions.DTOs;

public record ProductUomConversionDto
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public long FromUomId { get; set; }
    public string? FromUomCode { get; set; }
    public string? FromUomName { get; set; }
    public long ToUomId { get; set; }
    public string? ToUomCode { get; set; }
    public string? ToUomName { get; set; }
    public decimal ConversionFactor { get; set; }
    public string? Barcode { get; set; }
    public bool IsDefaultPurchase { get; set; }
    public bool IsDefaultSale { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
