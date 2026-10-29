using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Report.StockBalances.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Report.StockBalances.Queries.GetStockBalances;

public record GetStockBalancesQuery(
    long? CategoryId = null,
    string? Search = null,
    int? PageNumber = null,
    int? PageSize = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? ProductId = null,
    int? WarehouseId = null,
    bool ShowZeroStock = true,
    string? SortField = null,
    int Order = 1
) : IRequest<PagedResult<StockBalanceDto>>;
