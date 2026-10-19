using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.DeletePurchaseReturnDraft;

public class DeletePurchaseReturnCommandHandler : IRequestHandler<DeletePurchaseReturnCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeletePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeletePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var purchaseReturn = await _context.PurchaseReturns
            .Include(pr => pr.Items)
            .SingleOrDefaultAsync(pr => pr.Id == request.Id && pr.CompanyId == companyId
                && !pr.DeletedOn.HasValue, cancellationToken);
        if (purchaseReturn == null)
            throw new KeyNotFoundException("Purchase Return was not found.");
        if (purchaseReturn.Status != PurchaseReturnStatus.Draft)
            throw new InvalidOperationException("Only draft purchase returns can be deleted.");

        _context.PurchaseReturnItems.RemoveRange(purchaseReturn.Items.Where(i => !i.DeletedOn.HasValue));
        _context.PurchaseReturns.Remove(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
