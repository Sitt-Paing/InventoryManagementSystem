using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.ApprovePurchaseOrder;

public class ApprovePurchaseOrderCommandHandler : IRequestHandler<ApprovePurchaseOrderCommand, bool> 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    public ApprovePurchaseOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ApprovePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("You don't have permission to access.");
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        await _context.LockPurchaseOrderAsync(command.Id, cancellationToken);
        var purchaseOrder = await _context.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null)
        {
            return false;
        }

        purchaseOrder.Approve(userId, DateTime.Now);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}
