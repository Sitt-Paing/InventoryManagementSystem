using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.RejectPurchaseOrder;

public class RejectPurchaseOrderCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<RejectPurchaseOrderCommand, bool>
{
    public async Task<bool> Handle(RejectPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId)) throw new UnauthorizedAccessException("Authentication is required.");

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        await context.LockPurchaseOrderAsync(request.Id, cancellationToken);
        var order = await context.PurchaseOrders.FirstOrDefaultAsync(
            po => po.Id == request.Id && !po.DeletedOn.HasValue, cancellationToken);
        if (order == null) return false;

        order.Reject(userId, request.Reason, DateTime.Now);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
