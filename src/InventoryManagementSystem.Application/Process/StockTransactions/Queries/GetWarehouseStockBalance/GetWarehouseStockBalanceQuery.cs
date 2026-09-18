using MediatR;
using System;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetWarehouseStockBalance;

public record class GetWarehouseStockBalanceQuery(
    Guid ProductId,
    int WarehouseId
) : IRequest<decimal>;
