using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.DeletePurchaseOrder;

public class DeletePurchaseOrderCommandHandler : IRequestHandler<DeletePurchaseOrderCommand, PurchaseOrderDto?>
{
    private readonly IApplicationDbContext _context;

    public DeletePurchaseOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto?> Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        await using var databaseTransaction = await _context.BeginTransactionAsync(cancellationToken);

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null) return null;

        var haveReceipt = await _context.GoodReceipts.AnyAsync(x => x.PurchaseOrderId == purchaseOrder.Id && !x.DeletedOn.HasValue,cancellationToken);

        if (haveReceipt) throw new InvalidOperationException("Cannot delete a purchase order with active goods receipts.");

        var now = DateTime.UtcNow;
        purchaseOrder.DeletedOn = now;

        foreach (var item in purchaseOrder.Items)
        {
            item.DeletedOn = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await databaseTransaction.CommitAsync(cancellationToken);

        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            PurchaseOrderNo = purchaseOrder.PurchaseOrderNo,
            SupplierId = purchaseOrder.SupplierId,
            WarehouseId = purchaseOrder.WarehouseId,
            OrderDate = purchaseOrder.OrderDate,
            ExpectedDate = purchaseOrder.ExpectedDate,
            Status = purchaseOrder.Status,
            TotalAmount = purchaseOrder.TotalAmount,
            DeletedOn = purchaseOrder.DeletedOn
        };
    }
}
