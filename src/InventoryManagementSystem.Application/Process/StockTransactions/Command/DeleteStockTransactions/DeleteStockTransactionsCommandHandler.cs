using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.DeleteStockTransactions;

public class DeleteStockTransactionsCommandHandler : IRequestHandler<DeleteStockTransactionsCommand, StockTransactionsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteStockTransactionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<StockTransactionsDto?> Handle(DeleteStockTransactionsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.StockTransactions
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.DeletedOn.HasValue, cancellationToken);

        if (transaction == null)
        {
            return null;
        }

        var product = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == transaction.ProductId, cancellationToken);

        if (product != null)
        {
            // Revert stock impact
            if (string.Equals(transaction.TransactionType, "IN", StringComparison.OrdinalIgnoreCase))
            {
                if (product.CurrentStock < transaction.Quantity)
                {
                    throw new InvalidOperationException($"Cannot delete this Stock Intake (IN) transaction because current stock ({product.CurrentStock}) is lower than the movement quantity ({transaction.Quantity}).");
                }
                product.CurrentStock -= transaction.Quantity;
            }
            else if (string.Equals(transaction.TransactionType, "OUT", StringComparison.OrdinalIgnoreCase))
            {
                product.CurrentStock += transaction.Quantity;
            }
        }

        var effectiveUserId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : (_currentUserService.UserId ?? _currentUserService.UserName ?? "system");

        // Soft delete
        transaction.DeletedOn = DateTime.UtcNow;
        transaction.DeletedBy = effectiveUserId;

        await _context.SaveChangesAsync(cancellationToken);

        return new StockTransactionsDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = product?.Name,
            ProductSku = product?.Sku,
            UserId = transaction.UserId,
            WarehouseId = transaction.WarehouseId,
            WarehouseLocationId = transaction.WarehouseLocationId,
            Quantity = transaction.Quantity,
            TransactionType = transaction.TransactionType,
            TransactionDate = transaction.TransactionDate,
            ReferenceNo = transaction.ReferenceNo,
            Note = transaction.Note,
            CreatedOn = transaction.CreatedOn,
            CreatedBy = transaction.CreatedBy,
            UpdatedOn = transaction.UpdatedOn,
            UpdatedBy = transaction.UpdatedBy,
            DeletedOn = transaction.DeletedOn,
            DeletedBy = transaction.DeletedBy
        };
    }
}
