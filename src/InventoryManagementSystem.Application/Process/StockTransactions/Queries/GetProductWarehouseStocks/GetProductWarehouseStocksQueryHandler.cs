using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetProductWarehouseStocks;

public class GetProductWarehouseStocksQueryHandler : IRequestHandler<GetProductWarehouseStocksQuery, List<ProductWarehouseStockDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductWarehouseStocksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductWarehouseStockDto>> Handle(GetProductWarehouseStocksQuery request, CancellationToken cancellationToken)
    {
        return await (from w in _context.Warehouses.AsNoTracking().Where(w => !w.DeletedOn.HasValue)
                      join ws in _context.WarehouseStocks.AsNoTracking().Where(s => s.ProductId == request.ProductId && !s.DeletedOn.HasValue)
                          on w.Id equals ws.WarehouseId into wsGroup
                      from ws in wsGroup.DefaultIfEmpty()
                      orderby w.Name
                      select new ProductWarehouseStockDto
                      {
                          WarehouseId = w.Id,
                          WarehouseName = w.Name,
                          Quantity = ws != null ? ws.Quantity : 0m
                      }).ToListAsync(cancellationToken);
    }
}
