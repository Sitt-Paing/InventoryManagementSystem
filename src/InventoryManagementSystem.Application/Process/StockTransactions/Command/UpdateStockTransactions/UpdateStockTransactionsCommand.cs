using System;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.UpdateStockTransactions;

public record class UpdateStockTransactionsCommand(
    long Id,
    Guid ProductId,
    string? UserId,
    int WarehouseId,
    int WarehouseLocationId,
    decimal Quantity,
    string TransactionType,
    DateTime TransactionDate,
    long? ReferenceNo,
    string? Note,
    int? ToWarehouseId = null,
    int? ToWarehouseLocationId = null
) : IRequest<StockTransactionsDto?>;
