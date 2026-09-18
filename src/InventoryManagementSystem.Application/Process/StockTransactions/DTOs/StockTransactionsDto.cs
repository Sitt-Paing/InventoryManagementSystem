using System;

namespace InventoryManagementSystem.Application.Process.StockTransactions.DTOs;

public record class StockTransactionsDto
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }

    public string UserId { get; set; } = null!;

    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }

    public int WarehouseLocationId { get; set; }
    public string? WarehouseLocationName { get; set; }

    public decimal Quantity { get; set; }

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public long? ReferenceNo { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }
}

[Obsolete("Use StockTransactionsDto instead.")]
public record class StockTrasactionsDto : StockTransactionsDto;
