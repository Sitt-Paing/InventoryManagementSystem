using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssueById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssuePost;

public class CreateStockIssuePostCommandHandler : IRequestHandler<CreateStockIssuePostCommand, StockIssueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public CreateStockIssuePostCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<StockIssueDto> Handle(CreateStockIssuePostCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var issue = await _context.StockIssues.Include(issue => issue.Items)
            .SingleOrDefaultAsync(issue => issue.Id == request.StockIssueId && !issue.DeletedOn.HasValue, cancellationToken);
        if (issue == null)
            throw new KeyNotFoundException("Stock Issue was not found.");
        if (issue.Status == StockIssueStatus.Posted)
        {
            var existing = await _sender.Send(new GetStockIssueByIdQuery(issue.Id), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }
        if (issue.Status != StockIssueStatus.Draft)
            throw new InvalidOperationException("Only draft stock issues can be posted.");

        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Login is required to post a stock issue.");
        if (!await _context.Warehouses.AnyAsync(w => w.Id == issue.WarehouseId && w.Status && !w.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The selected warehouse is unavailable.");
        if (!await _context.Departments.AnyAsync(d => d.Id == issue.DepartmentId && d.IsActive && !d.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The selected department is unavailable.");

        var items = issue.Items.Where(item => !item.DeletedOn.HasValue).OrderBy(item => item.ProductId).ToList();
        if (items.Count == 0 || items.Any(item => item.Quantity <= 0))
            throw new InvalidOperationException("At least one item with a positive quantity is required.");

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(product => productIds.Contains(product.Id)
            && product.Status && !product.DeletedOn.HasValue).OrderBy(product => product.Id).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more selected products are unavailable.");

        foreach (var group in items.GroupBy(item => item.ProductId))
        {
            var required = group.Sum(item => item.Quantity);
            var stock = await _context.WarehouseStocks.SingleOrDefaultAsync(stock => stock.ProductId == group.Key
                && stock.WarehouseId == issue.WarehouseId && !stock.DeletedOn.HasValue, cancellationToken);
            if (stock == null || stock.Quantity < required)
            {
                var productName = products.Single(product => product.Id == group.Key).Name;
                throw new InvalidOperationException($"Insufficient warehouse stock for '{productName}'. Available: {stock?.Quantity ?? 0}, required: {required}.");
            }
            stock.Quantity -= required;
        }

        var now = DateTime.Now;
        foreach (var item in items)
        {
            _context.StockTransactions.Add(new StockTransaction
            {
                ProductId = item.ProductId,
                WarehouseId = issue.WarehouseId,
                WarehouseLocationId = 0,
                Quantity = item.Quantity,
                TransactionType = "OUT",
                TransactionDate = issue.IssueDate,
                UserId = userId,
                StockIssueId = issue.Id,
                StockIssueItemId = item.Id,
                Note = $"Stock Issue: {issue.IssueNo}"
            });
        }
        issue.Status = StockIssueStatus.Posted;
        issue.PostedBy = userId;
        issue.PostedOn = now;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CurrentStock = await _context.WarehouseStocks
                .Where(stock => stock.ProductId == product.Id && !stock.DeletedOn.HasValue)
                .SumAsync(stock => stock.Quantity, cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);
        var result = await _sender.Send(new GetStockIssueByIdQuery(issue.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
