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

public class GetStockTransactionsQueryHandler : IRequestHandler<GetStockTransactionsQuery, List<StockTrasactionsDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockTrasactionsDto>> Handle(GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StockTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => !t.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.TransactionType))
        {
            query = query.Where(t => t.TransactionType == request.TransactionType.ToUpper());
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
            var targetDate = request.Date.Value.Date;
            query = query.Where(t => t.TransactionDate.Date == targetDate);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new StockTrasactionsDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product != null ? t.Product.Name : null,
                ProductSku = t.Product != null ? t.Product.Sku : null,
                UserId = t.UserId,
                WarehouseId = t.WarehouseId,
                WarehouseLocationId = t.WarehouseLocationId,
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
            })
            .ToListAsync(cancellationToken);
    }
}
