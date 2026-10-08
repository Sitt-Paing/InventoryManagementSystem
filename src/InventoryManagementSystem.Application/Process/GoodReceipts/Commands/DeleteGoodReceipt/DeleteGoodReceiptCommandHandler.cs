using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.DeleteGoodReceipt;

public class DeleteGoodReceiptCommandHandler : IRequestHandler<DeleteGoodReceiptCommand, GoodReceiptDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteGoodReceiptCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GoodReceiptDto?> Handle(DeleteGoodReceiptCommand request, CancellationToken cancellationToken)
    {
        var orderId = await _context.GoodReceipts.AsNoTracking()
            .Where(gr => gr.Id == request.Id && !gr.DeletedOn.HasValue)
            .Select(gr => (Guid?)gr.PurchaseOrderId).FirstOrDefaultAsync(cancellationToken);
        if (!orderId.HasValue) return null;
        await using var databaseTransaction = await _context.BeginTransactionAsync(cancellationToken);
        await _context.LockPurchaseOrderAsync(orderId.Value, cancellationToken);

        var goodReceipt = await _context.GoodReceipts
            .Include(gr => gr.Items)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && !gr.DeletedOn.HasValue, cancellationToken);

        if (goodReceipt == null)
        {
            return null;
        }

        var now = DateTime.Now;
        var userId = _currentUserService.UserName ?? _currentUserService.UserId ?? "System";

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == goodReceipt.PurchaseOrderId, cancellationToken);

        if (purchaseOrder == null || purchaseOrder.DeletedOn.HasValue)
        {
            throw new InvalidOperationException("The receipt's purchase order is unavailable.");
        }

        foreach (var item in goodReceipt.Items.Where(i => !i.DeletedOn.HasValue))
        {
            if (!item.ReceivedBaseQuantity.HasValue)
            {
                throw new InvalidOperationException ($"Receipt item '{item.Id}' has no recorded base quantity.");
            }
            var baseQuantity = item.ReceivedBaseQuantity.Value;
            if (baseQuantity <= 0 || item.ReceivedQuantity <= 0)
            {
                throw new InvalidOperationException($"Receipt item '{item.Id}' has an invalid recorded quantity.");
            }

            var originalTransaction = await _context.StockTransactions.SingleOrDefaultAsync(
                x => x.GoodReceiptId == goodReceipt.Id && x.GoodReceiptItemId == item.Id
                    && x.TransactionType == "IN" && !x.DeletedOn.HasValue
                    && !x.ReversesStockTransactionId.HasValue, cancellationToken);

            if (originalTransaction == null)
            {
                throw new InvalidOperationException($"Receipt item '{item.Id}' has no linked original stock movement. Its history must be reconciled before deletion.");
            }

            if (await _context.StockTransactions.AnyAsync(x => x.ReversesStockTransactionId == originalTransaction.Id, cancellationToken))
            {
                throw new InvalidOperationException($"Receipt item '{item.Id}' has already been reversed.");
            }

            if (originalTransaction.ProductId != item.ProductId
                || originalTransaction.WarehouseId != goodReceipt.WarehouseId
                || originalTransaction.Quantity != baseQuantity)
            {
                throw new InvalidOperationException($"Receipt item '{item.Id}' does not match its original stock movement.");
            }
            // Revert PurchaseOrderItem received quantity
            var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == item.PurchaseOrderItemId && !i.DeletedOn.HasValue);
            if (poItem == null || poItem.ProductId != item.ProductId || poItem.UomId != item.UomId
                || poItem.ReceivedQuantity < item.ReceivedQuantity)
            {
                throw new InvalidOperationException($"Receipt item '{item.Id}' does not match the purchase order's received quantity.");
            }
            poItem.ReceivedQuantity -= item.ReceivedQuantity;

            // Revert Warehouse stock
            var stock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.WarehouseId == goodReceipt.WarehouseId && !s.DeletedOn.HasValue, cancellationToken);

            if(stock == null)
            {
                throw new InvalidOperationException($"Warehouse stock for product '{item.ProductId}' in warehouse '{goodReceipt.WarehouseId}' not found.");
            } else if(stock.Quantity < baseQuantity)
            {
                throw new InvalidOperationException($"Cannot delete receipt item '{item.Id}' as it would result in negative stock for product '{item.ProductId}' in warehouse '{goodReceipt.WarehouseId}'.");
            } else
            {
                stock.Quantity -= baseQuantity;
            }

            _context.StockTransactions.Add(new StockTransaction
            {
                CompanyId = originalTransaction.CompanyId,
                ProductId = item.ProductId,
                WarehouseId = originalTransaction.WarehouseId,
                WarehouseLocationId = originalTransaction.WarehouseLocationId,
                Quantity = baseQuantity,
                TransactionType = "OUT",
                TransactionDate = now,
                UserId = userId,
                GoodReceiptId = goodReceipt.Id,
                GoodReceiptItemId = item.Id,
                ReversesStockTransactionId = originalTransaction.Id,
                Note = $"Reversal of goods receipt '{goodReceipt.ReceiptNo}', stock transaction '{originalTransaction.Id}'."
            });
            item.DeletedOn = now;
            item.DeletedBy = userId;
            
        }

        goodReceipt.DeletedOn = now;
        goodReceipt.DeletedBy = userId;

        if (purchaseOrder.Status != PurchaseOrderStatus.Cancelled)
        {
            // Recalculate PO status
            var activePoItems = purchaseOrder.Items.Where(i => !i.DeletedOn.HasValue).ToList();
            if (activePoItems.Count > 0 && activePoItems.All(i => i.ReceivedQuantity >= i.Quantity))
            {
                purchaseOrder.Status = PurchaseOrderStatus.Completed;
            }
            else if (activePoItems.Any(i => i.ReceivedQuantity > 0))
            {
                purchaseOrder.Status = PurchaseOrderStatus.PartiallyReceived;
            }
            else
            {
                purchaseOrder.Status = PurchaseOrderStatus.Pending;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Sync Product CurrentStock
        var productIds = goodReceipt.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CurrentStock = await _context.WarehouseStocks
                .Where(w => w.ProductId == product.Id && !w.DeletedOn.HasValue)
                .SumAsync(w => w.Quantity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        return new GoodReceiptDto
        {
            Id = goodReceipt.Id,
            ReceiptNo = goodReceipt.ReceiptNo,
            WarehouseId = goodReceipt.WarehouseId,
            PurchaseOrderId = goodReceipt.PurchaseOrderId,
            SupplierId = goodReceipt.SupplierId,
            ReceiptDate = goodReceipt.ReceiptDate,
            Status = goodReceipt.Status,
            ReceivedBy = goodReceipt.ReceivedBy,
            Note = goodReceipt.Note,
            DeletedOn = goodReceipt.DeletedOn
        };
    }
}
