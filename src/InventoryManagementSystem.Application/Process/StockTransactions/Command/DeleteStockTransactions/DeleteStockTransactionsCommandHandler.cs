using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.DeleteStockTransactions;

public class DeleteStockTransactionsCommandHandler : IRequestHandler<DeleteStockTransactionsCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteStockTransactionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteStockTransactionsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.StockTransactions
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.DeletedOn.HasValue, cancellationToken);

        if (transaction == null)
        {
            throw new KeyNotFoundException($"Stock transaction with ID {request.Id} not found.");
        }

        var product = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == transaction.ProductId, cancellationToken);

        if (product != null)
        {
            // Revert stock impact
            if (transaction.TransactionType == "IN")
            {
                if (product.CurrentStock < transaction.Quantity)
                {
                    throw new InvalidOperationException($"Cannot delete this Stock Intake (IN) transaction because current stock ({product.CurrentStock}) is lower than the movement quantity ({transaction.Quantity}).");
                }
                product.CurrentStock -= transaction.Quantity;
            }
            else if (transaction.TransactionType == "OUT")
            {
                product.CurrentStock += transaction.Quantity;
            }
        }

        // Soft delete
        transaction.DeletedOn = DateTime.UtcNow;
        transaction.DeletedBy = request.UserId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
