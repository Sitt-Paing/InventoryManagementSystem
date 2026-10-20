using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.DeleteStockIssueDraft;

public class DeleteStockIssueCommandHandler : IRequestHandler<DeleteStockIssueCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteStockIssueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteStockIssueCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var issue = await _context.StockIssues.Include(issue => issue.Items)
            .SingleOrDefaultAsync(issue => issue.Id == request.Id && !issue.DeletedOn.HasValue, cancellationToken);
        if (issue == null)
            throw new KeyNotFoundException("Stock Issue was not found.");
        if (issue.Status != StockIssueStatus.Draft)
            throw new InvalidOperationException("Posted stock issues cannot be deleted.");

        // SaveChangesAsync converts Deleted entries to soft deletes and fills DeletedOn/DeletedBy.
        _context.StockIssueItems.RemoveRange(issue.Items.Where(item => !item.DeletedOn.HasValue));
        _context.StockIssues.Remove(issue);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
