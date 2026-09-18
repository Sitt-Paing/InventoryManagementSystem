using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;

public record class GetStockTransactionsQuery(
    string? TransactionType = null,
    DateTime? Date = null,
    Guid? ProductId = null,
    int? WarehouseId = null
) : IRequest<List<StockTransactionsDto>>;
