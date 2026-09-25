using System;
using System.Collections.Generic;
using InventoryManagementSystem.Domain.Enums;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierCode { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems => Items?.Count ?? 0;
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}
