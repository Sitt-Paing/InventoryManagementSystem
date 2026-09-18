using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactionById;

public class GetStockTransactionByIdQueryHandler : IRequestHandler<GetStockTransactionByIdQuery, StockTransactionsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockTransactionsDto?> Handle(GetStockTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await (from t in _context.StockTransactions.AsNoTracking()
                                 where t.Id == request.Id && !t.DeletedOn.HasValue
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
                                 }).FirstOrDefaultAsync(cancellationToken);

        return transaction;
    }
}
