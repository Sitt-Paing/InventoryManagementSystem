using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SubmitPurchaseOrder;

public class SubmitPurchaseOrderHandler : IRequestHandler<SubmitPurchaseOrderCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public SubmitPurchaseOrderHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        await using var databaseTransaction = await _context.BeginTransactionAsync(cancellationToken);

        await _context.LockPurchaseOrderAsync(command.Id, cancellationToken);

        PurchaseOrder? purchaseOrder = await _context.PurchaseOrders.Include(po => po.Items).FirstOrDefaultAsync(x => x.Id == command.Id && !x.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null) return false;

        purchaseOrder.SubmitForApproval();
        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        return true;
    }
}
