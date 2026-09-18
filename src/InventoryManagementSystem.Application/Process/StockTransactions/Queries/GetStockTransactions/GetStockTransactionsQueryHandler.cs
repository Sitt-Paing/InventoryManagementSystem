using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;

public class GetStockTransactionsQueryHandler : IRequestHandler<GetStockTransactionsQuery, List<StockTransactionsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockTransactionsDto>> Handle(GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StockTransactions
            .AsNoTracking()
            .Where(t => !t.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.TransactionType))
        {
            var normalizedType = request.TransactionType.Trim().ToUpperInvariant();
            query = query.Where(t => t.TransactionType == normalizedType);
        }

        if (request.ProductId.HasValue && request.ProductId.Value != Guid.Empty)
        {
            query = query.Where(t => t.ProductId == request.ProductId.Value);
        }

        if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0)
        {
            query = query.Where(t => t.WarehouseId == request.WarehouseId.Value);
        }

        if (request.Date.HasValue)
        {
            var startDate = request.Date.Value.Date;
            var endDate = startDate.AddDays(1);
            query = query.Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate);
        }

        return await (from t in query
                      join w in _context.Warehouses.AsNoTracking() on t.WarehouseId equals w.Id into whGroup
                      from wh in whGroup.DefaultIfEmpty()
                      join l in _context.WarehouseLocations.AsNoTracking() on t.WarehouseLocationId equals l.Id into locGroup
                      from loc in locGroup.DefaultIfEmpty()
                      join tw in _context.Warehouses.AsNoTracking() on t.ToWarehouseId equals (int?)tw.Id into toWhGroup
                      from toWh in toWhGroup.DefaultIfEmpty()
                      join tl in _context.WarehouseLocations.AsNoTracking() on t.ToWarehouseLocationId equals (int?)tl.Id into toLocGroup
                      from toLoc in toLocGroup.DefaultIfEmpty()
                      orderby t.TransactionDate descending
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
                      }).ToListAsync(cancellationToken);
    }
}
