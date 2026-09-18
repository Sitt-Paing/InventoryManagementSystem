using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetWarehouseStockBalance;

public class GetWarehouseStockBalanceQueryHandler : IRequestHandler<GetWarehouseStockBalanceQuery, decimal>
{
    private readonly IApplicationDbContext _context;

    public GetWarehouseStockBalanceQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> Handle(GetWarehouseStockBalanceQuery request, CancellationToken cancellationToken)
    {
        var stock = await _context.WarehouseStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId && x.WarehouseId == request.WarehouseId && !x.DeletedOn.HasValue, cancellationToken);

        return stock?.Quantity ?? 0m;
    }
}
