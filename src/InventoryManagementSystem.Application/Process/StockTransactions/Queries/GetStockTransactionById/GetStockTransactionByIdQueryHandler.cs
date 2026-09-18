using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactionById;

public class GetStockTransactionByIdQueryHandler : IRequestHandler<GetStockTransactionByIdQuery, StockTrasactionsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetStockTransactionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockTrasactionsDto?> Handle(GetStockTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _context.StockTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => t.Id == request.Id && !t.DeletedOn.HasValue)
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
            .FirstOrDefaultAsync(cancellationToken);

        return transaction;
    }
}
