using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.DeleteGoodReceipt;

public class DeleteGoodReceiptCommandHandler : IRequestHandler<DeleteGoodReceiptCommand, GoodReceiptDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteGoodReceiptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GoodReceiptDto?> Handle(DeleteGoodReceiptCommand request, CancellationToken cancellationToken)
    {
        var goodReceipt = await _context.GoodReceipts
            .Include(gr => gr.Items)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && !gr.DeletedOn.HasValue, cancellationToken);

        if (goodReceipt == null)
        {
            return null;
        }

        var now = DateTime.Now;
        goodReceipt.DeletedOn = now;

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == goodReceipt.PurchaseOrderId, cancellationToken);

        foreach (var item in goodReceipt.Items.Where(i => !i.DeletedOn.HasValue))
        {
            item.DeletedOn = now;

            // Revert PurchaseOrderItem received quantity
            if (purchaseOrder != null)
            {
                var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == item.PurchaseOrderItemId);
                if (poItem != null)
                {
                    poItem.ReceivedQuantity = Math.Max(0, poItem.ReceivedQuantity - item.ReceivedQuantity);
                }
            }

            // Revert Warehouse stock
            var stock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.WarehouseId == goodReceipt.WarehouseId && !s.DeletedOn.HasValue, cancellationToken);

            if (stock != null)
            {
                stock.Quantity = Math.Max(0, stock.Quantity - item.ReceivedQuantity);
            }
        }

        if (purchaseOrder != null)
        {
            // Recalculate PO status
            if (purchaseOrder.Items.All(i => i.ReceivedQuantity >= i.Quantity))
            {
                purchaseOrder.Status = PurchaseOrderStatus.Completed;
            }
            else if (purchaseOrder.Items.Any(i => i.ReceivedQuantity > 0))
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
