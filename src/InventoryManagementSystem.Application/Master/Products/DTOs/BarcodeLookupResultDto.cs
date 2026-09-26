using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.Products.DTOs;

public class BarcodeLookupResultDto
{
    public string LookupType { get; set; } = "Product"; // "Product" or "Location"
    public ProductBarcodeLookupDto? Product { get; set; }
    public LocationBarcodeLookupDto? Location { get; set; }
}

public class ProductBarcodeLookupDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Brand { get; set; }
    public string? CategoryName { get; set; }
    public string? Unit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public bool Status { get; set; }
    public string? Description { get; set; }

    public List<ProductLocationDetailDto> Locations { get; set; } = new();
    public List<ProductWarehouseSummaryDto> WarehouseStocks { get; set; } = new();
    public List<ProductRecentMovementDto> RecentMovements { get; set; } = new();
}

public class ProductLocationDetailDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public int? WarehouseLocationId { get; set; }
    public string LocationCode { get; set; } = null!;
    public string? Zone { get; set; }
    public string? Rack { get; set; }
    public string? Bin { get; set; }
    public string? LocationBarcode { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? LastMovementDate { get; set; }
    public bool IsGeneralArea { get; set; }
}

public class ProductWarehouseSummaryDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public decimal Quantity { get; set; }
}

public class ProductRecentMovementDto
{
    public long Id { get; set; }
    public string TransactionType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public DateTime TransactionDate { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string? LocationCode { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? ToLocationCode { get; set; }
    public string? UserId { get; set; }
    public string? Note { get; set; }
}

public class LocationBarcodeLookupDto
{
    public int WarehouseLocationId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string LocationCode { get; set; } = null!;
    public string? Zone { get; set; }
    public string? Rack { get; set; }
    public string? Bin { get; set; }
    public string? Barcode { get; set; }
    public decimal? Capacity { get; set; }
    public bool Status { get; set; }
    public List<LocationProductItemDto> Products { get; set; } = new();
}

public class LocationProductItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? LastMovementDate { get; set; }
}
