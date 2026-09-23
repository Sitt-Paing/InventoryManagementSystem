using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;

public class GetStockTransactionsQueryHandler : IRequestHandler<GetStockTransactionsQuery, PagedResult<StockTransactionsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StockTransactionsDto>> Handle(GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = StockTransactionQuery(
            request.StartDate,
            request.EndDate,
            request.Date,
            request.Q,
            request.SortField,
            request.Order,
            request.TransactionType,
            request.WarehouseId,
            request.ProductId);

        var totalRecords = await query.CountAsync(cancellationToken);

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockTransactionsDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private IQueryable<StockTransactionsDto> StockTransactionQuery(
        DateTime? sDate,
        DateTime? eDate,
        DateTime? date,
        string? q,
        string? sortField,
        int order,
        string? status,
        int? warehouseId,
        Guid? productId)
    {
        IQueryable<Domain.Entities.StockTransaction> query = _context.StockTransactions
            .AsNoTracking()
            .Where(t => !t.DeletedOn.HasValue);

        // Movement / Status switch
        var movementType = status?.Trim().ToUpperInvariant();
        query = movementType switch
        {
            "IN" => query.Where(t => t.TransactionType == "IN"),
            "OUT" => query.Where(t => t.TransactionType == "OUT"),
            "TRANSFER" => query.Where(t => t.TransactionType == "TRANSFER"),
            "ADJUSTMENT" => query.Where(t => t.TransactionType == "ADJUSTMENT"),
            _ => query
        };

        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }

        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value || t.ToWarehouseId == warehouseId.Value);
        }

        // Date range filtering
        if (sDate.HasValue || eDate.HasValue)
        {
            if (sDate.HasValue)
            {
                var start = sDate.Value.Date;
                query = query.Where(t => t.TransactionDate >= start);
            }

            if (eDate.HasValue)
            {
                var end = eDate.Value.Date.AddDays(1);
                query = query.Where(t => t.TransactionDate < end);
            }
        }
        else if (date.HasValue)
        {
            var startDate = date.Value.Date;
            var endDate = startDate.AddDays(1);
            query = query.Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate);
        }
        else
        {
            // Default to today's movement data
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            query = query.Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow);
        }

        var projectedQuery = from t in query
                             join w in _context.Warehouses.AsNoTracking() on t.WarehouseId equals w.Id into whGroup
                             from wh in whGroup.DefaultIfEmpty()
                             join l in _context.WarehouseLocations.AsNoTracking() on t.WarehouseLocationId equals l.Id into locGroup
                             from loc in locGroup.DefaultIfEmpty()
                             join tw in _context.Warehouses.AsNoTracking() on t.ToWarehouseId equals (int?)tw.Id into toWhGroup
                             from toWh in toWhGroup.DefaultIfEmpty()
                             join tl in _context.WarehouseLocations.AsNoTracking() on t.ToWarehouseLocationId equals (int?)tl.Id into toLocGroup
                             from toLoc in toLocGroup.DefaultIfEmpty()
                             select new StockTransactionsDto
                             {
                                 Id = t.Id,
                                 ProductId = t.ProductId,
                                 ProductName = t.Product != null ? t.Product.Name : null,
                                 ProductSku = t.Product != null ? t.Product.Sku : null,
                                 UserId = t.UserId,
                                 WarehouseId = t.WarehouseId,
                                 WarehouseName = wh != null ? wh.Name : null,
                                 WarehouseLocationId = t.WarehouseLocationId,
                                 WarehouseLocationName = loc != null ? loc.LocationCode : null,
                                 ToWarehouseId = t.ToWarehouseId,
                                 ToWarehouseName = toWh != null ? toWh.Name : null,
                                 ToWarehouseLocationId = t.ToWarehouseLocationId,
                                 ToWarehouseLocationName = toLoc != null ? toLoc.LocationCode : null,
                                 Quantity = t.Quantity,
                                 TransactionType = t.TransactionType,
                                 TransactionDate = t.TransactionDate,
                                 ReferenceNo = t.ReferenceNo,
                                 Note = t.Note,
                                 CreatedOn = t.CreatedOn,
                                 CreatedBy = t.CreatedBy,
                                 UpdatedOn = t.UpdatedOn,
                                 UpdatedBy = t.UpdatedBy,
                                 DeletedOn = t.DeletedOn,
                                 DeletedBy = t.DeletedBy
                             };

        // Filtering by search keyword q
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            projectedQuery = projectedQuery.Where(x =>
                (x.ProductName ?? string.Empty).ToLower().Contains(term) ||
                (x.ProductSku ?? string.Empty).ToLower().Contains(term) ||
                (x.WarehouseName ?? string.Empty).ToLower().Contains(term) ||
                (x.ToWarehouseName ?? string.Empty).ToLower().Contains(term) ||
                (x.WarehouseLocationName ?? string.Empty).ToLower().Contains(term) ||
                (x.Note ?? string.Empty).ToLower().Contains(term) ||
                (x.CreatedBy ?? string.Empty).ToLower().Contains(term) ||
                (x.ReferenceNo.HasValue && x.ReferenceNo.Value.ToString().Contains(term)) ||
                x.Id.ToString().Contains(term)
            );
        }

        // Sorting
        projectedQuery = (sortField?.ToLower(), order > 0) switch
        {
            ("id", true) => projectedQuery.OrderBy(x => x.Id),
            ("id", false) => projectedQuery.OrderByDescending(x => x.Id),
            ("productname", true) => projectedQuery.OrderBy(x => x.ProductName),
            ("productname", false) => projectedQuery.OrderByDescending(x => x.ProductName),
            ("productsku", true) => projectedQuery.OrderBy(x => x.ProductSku),
            ("productsku", false) => projectedQuery.OrderByDescending(x => x.ProductSku),
            ("transactiontype", true) => projectedQuery.OrderBy(x => x.TransactionType),
            ("transactiontype", false) => projectedQuery.OrderByDescending(x => x.TransactionType),
            ("quantity", true) => projectedQuery.OrderBy(x => x.Quantity),
            ("quantity", false) => projectedQuery.OrderByDescending(x => x.Quantity),
            ("referenceno", true) => projectedQuery.OrderBy(x => x.ReferenceNo),
            ("referenceno", false) => projectedQuery.OrderByDescending(x => x.ReferenceNo),
            ("transactiondate", true) => projectedQuery.OrderBy(x => x.TransactionDate),
            ("transactiondate", false) => projectedQuery.OrderByDescending(x => x.TransactionDate),
            ("warehousename", true) => projectedQuery.OrderBy(x => x.WarehouseName),
            ("warehousename", false) => projectedQuery.OrderByDescending(x => x.WarehouseName),
            ("createdon", true) => projectedQuery.OrderBy(x => x.CreatedOn),
            ("createdon", false) => projectedQuery.OrderByDescending(x => x.CreatedOn),
            _ => projectedQuery.OrderByDescending(x => x.CreatedOn)
        };

        return projectedQuery;
    }
}
