using System;

namespace InventoryManagementSystem.Application.WarehouseLocations.DTOs;

public record class WarehouseLocationDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseCode { get; set; }
    public string LocationCode { get; set; } = null!;
    public string? Zone { get; set; }
    public string? Rack { get; set; }
    public string? Bin { get; set; }
    public string? Barcode { get; set; }
    public decimal? Capacity { get; set; }
    public bool Status { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
