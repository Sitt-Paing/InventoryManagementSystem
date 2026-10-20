using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssueById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.UpdateStockIssueDraft;

public class UpdateStockIssueCommandHandler : IRequestHandler<UpdateStockIssueCommand, StockIssueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public UpdateStockIssueCommandHandler(IApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<StockIssueDto> Handle(UpdateStockIssueCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var issue = await _context.StockIssues.Include(issue => issue.Items)
            .SingleOrDefaultAsync(issue => issue.Id == request.Id && !issue.DeletedOn.HasValue, cancellationToken);
        if (issue == null)
            throw new KeyNotFoundException("Stock Issue was not found.");
        if (issue.Status != StockIssueStatus.Draft)
            throw new InvalidOperationException("Posted stock issues cannot be updated.");

        if (!await _context.Warehouses.AnyAsync(w => w.Id == request.WarehouseId && w.Status && !w.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The selected warehouse is unavailable.");
        if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.IsActive && !d.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The selected department is unavailable.");

        var productIds = request.Items.Select(item => item.ProductId).ToList();
        var productCount = await _context.Products.CountAsync(product => productIds.Contains(product.Id)
            && product.Status && !product.DeletedOn.HasValue, cancellationToken);
        if (productCount != productIds.Count)
            throw new InvalidOperationException("One or more selected products are unavailable.");

        issue.IssueDate = request.IssueDate;
        issue.WarehouseId = request.WarehouseId;
        issue.DepartmentId = request.DepartmentId;
        issue.ReceivedBy = request.ReceivedBy.Trim();
        issue.Note = request.Note?.Trim();
        foreach (var item in issue.Items.Where(item => !item.DeletedOn.HasValue && !productIds.Contains(item.ProductId)))
            _context.StockIssueItems.Remove(item);
        foreach (var input in request.Items)
        {
            var item = issue.Items.SingleOrDefault(item => item.ProductId == input.ProductId);
            if (item == null)
            {
                item = new StockIssueItem { ProductId = input.ProductId };
                issue.Items.Add(item);
            }
            item.Quantity = input.Quantity;
            item.DeletedOn = null;
            item.DeletedBy = null;
        }

        // Mark the header as changed even when only item quantities were edited.
        issue.UpdatedOn = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);
        var result = await _sender.Send(new GetStockIssueByIdQuery(issue.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
