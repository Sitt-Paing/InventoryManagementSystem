using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Report.StockBalances.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Report.StockBalances.Queries.GetStockBalances;

public class GetStockBalancesQueryHandler : IRequestHandler<GetStockBalancesQuery, PagedResult<StockBalanceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockBalancesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockBalanceDto>> Handle(GetStockBalancesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StockBalances.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.ProductName.Contains(search) 
                || (x.Sku != null && x.Sku.Contains(search))
                || x.WarehouseName.Contains(search)
                || x.CategoryName.Contains(search));
        }
        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }
        if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0)
        {
            query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);
        }
        if (!request.ShowZeroStock)
        {
            query = query.Where(x => x.Quantity != 0);
        }

        // Dates filter stock row creation, not historical stock balances.
        if (request.StartDate.HasValue)
        {
            var startDate = request.StartDate.Value.Date;
            query = query.Where(x => x.StockCreatedOn >= startDate);
        }
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.Date;
            query = query.Where(x => x.StockCreatedOn.HasValue && x.StockCreatedOn.Value.Date <= endDate);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var orderedQuery = request.SortField?.ToLowerInvariant() switch
        {
            "sku" => request.Order == 1 ? query.OrderBy(x => x.Sku) : query.OrderByDescending(x => x.Sku),
            "productname" => request.Order == 1 ? query.OrderBy(x => x.ProductName) : query.OrderByDescending(x => x.ProductName),
            "categoryname" => request.Order == 1 ? query.OrderBy(x => x.CategoryName) : query.OrderByDescending(x => x.CategoryName),
            "warehousename" => request.Order == 1 ? query.OrderBy(x => x.WarehouseName) : query.OrderByDescending(x => x.WarehouseName),
            "quantity" => request.Order == 1 ? query.OrderBy(x => x.Quantity) : query.OrderByDescending(x => x.Quantity),
            "stockcreatedon" => request.Order == 1 ? query.OrderBy(x => x.StockCreatedOn) : query.OrderByDescending(x => x.StockCreatedOn),
            "stockupdatedon" => request.Order == 1 ? query.OrderBy(x => x.StockUpdatedOn) : query.OrderByDescending(x => x.StockUpdatedOn),
            _ => query.OrderBy(x => x.ProductName)
        };

        query = orderedQuery.ThenBy(x => x.WarehouseStockId);

        if (request.PageNumber.HasValue && request.PageSize.HasValue)
        {
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                .Take(request.PageSize.Value);
        }

        var stockBalances = await query
            .Select(s => new StockBalanceDto
            {
                WarehouseStockId = s.WarehouseStockId,
                CompanyId = s.CompanyId,
                ProductId = s.ProductId,
                Sku = s.Sku,
                ProductName = s.ProductName,
                CategoryId = s.CategoryId,
                CategoryName = s.CategoryName,
                WarehouseId = s.WarehouseId,
                WarehouseCode = s.WarehouseCode,
                WarehouseName = s.WarehouseName,
                Quantity = s.Quantity,
                UomName = s.UomName,
                UomSymbol = s.UomSymbol,
                StockCreatedOn = s.StockCreatedOn,
                StockCreatedBy = s.StockCreatedBy,
                StockUpdatedOn = s.StockUpdatedOn,
                StockUpdatedBy = s.StockUpdatedBy,
                StockDeletedOn = s.StockDeletedOn,
                StockDeletedBy = s.StockDeletedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StockBalanceDto>
        {
            Items = stockBalances,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalRecords
        };
    }
}
