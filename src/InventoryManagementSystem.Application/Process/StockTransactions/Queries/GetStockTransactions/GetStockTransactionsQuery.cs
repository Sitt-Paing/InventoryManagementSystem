using System;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;

public record class GetStockTransactionsQuery(
    string? TransactionType = null,
    DateTime? Date = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Q = null,
    string? SortField = null,
    int Order = -1,
    Guid? ProductId = null,
    int? WarehouseId = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<StockTransactionsDto>>;
