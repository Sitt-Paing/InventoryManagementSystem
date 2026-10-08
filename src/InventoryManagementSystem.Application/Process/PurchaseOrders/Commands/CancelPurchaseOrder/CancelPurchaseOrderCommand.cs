using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CancelPurchaseOrder;

public record CancelPurchaseOrderCommand(Guid Id, string Reason) : IRequest<bool>;

public class CancelPurchaseOrderCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CancelPurchaseOrderCommand, bool>
{
    public async Task<bool> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw new InvalidOperationException("A cancellation reason of 1 to 500 characters is required.");

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        await context.LockPurchaseOrderAsync(request.Id, cancellationToken);
        var order = await context.PurchaseOrders.Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.DeletedOn.HasValue, cancellationToken);
        if (order == null) return false;
        if (order.Status == PurchaseOrderStatus.Cancelled) return true;
        if (order.Status == PurchaseOrderStatus.Completed || order.Items.Any(i => !i.DeletedOn.HasValue && i.ReceivedQuantity > 0)
            || await context.GoodReceipts.AnyAsync(r => r.PurchaseOrderId == order.Id && !r.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("Only an unreceived purchase order can be cancelled. Reverse its receipts first.");

        order.Status = PurchaseOrderStatus.Cancelled;
        order.CancellationReason = request.Reason.Trim();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
