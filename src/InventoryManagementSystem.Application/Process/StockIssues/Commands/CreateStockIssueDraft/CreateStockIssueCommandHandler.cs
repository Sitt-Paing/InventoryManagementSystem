using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;

public class CreateStockIssueCommandHandler : IRequestHandler<CreateStockIssueCommand, StockIssueDto>
{
    private readonly IApplicationDbContext _context;
    public CreateStockIssueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockIssueDto> Handle(CreateStockIssueCommand command, CancellationToken cancellationToken)
    {
        var issueNo = command.IssueNo.Trim();
        if (await _context.StockIssues.AnyAsync(issue => issue.IssueNo == issueNo, cancellationToken))
        {
            throw new InvalidOperationException("Issue number is already in use.");
        }

        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.Id == command.WarehouseId && w.Status && !w.DeletedOn.HasValue,
                cancellationToken);

        if (warehouse == null)
        {
            throw new InvalidOperationException("The selected warehouse is unavailable.");
        }

        var department = await _context.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == command.DepartmentId && d.IsActive && !d.DeletedOn.HasValue,
                cancellationToken);

        if (department == null)
        {
            throw new InvalidOperationException("The selected department is unavailable.");
        }

        var productIds = command.Items.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id) && product.Status && !product.DeletedOn.HasValue)
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.BaseUomId,
                UomName = product.BaseUom.Name
            })
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("One or more selected products are unavailable.");
        }

        var stockIssue = new StockIssue
        {
            Id = Guid.NewGuid(),
            IssueNo = issueNo,
            IssueDate = command.IssueDate,
            WarehouseId = command.WarehouseId,
            DepartmentId = command.DepartmentId,
            ReceivedBy = command.ReceivedBy.Trim(),
            Note = command.Note?.Trim(),
            Status = StockIssueStatus.Draft,
            Items = command.Items.Select(item => new StockIssueItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList()
        };

        _context.StockIssues.Add(stockIssue);
        await _context.SaveChangesAsync(cancellationToken);

        return new StockIssueDto
        {
            Id = stockIssue.Id,
            IssueNo = stockIssue.IssueNo,
            IssueDate = stockIssue.IssueDate,
            WarehouseId = stockIssue.WarehouseId,
            WarehouseName = warehouse.Name,
            DepartmentId = stockIssue.DepartmentId,
            DepartmentName = department.DeptName,
            ReceivedBy = stockIssue.ReceivedBy,
            Note = stockIssue.Note,
            Status = stockIssue.Status,
            CreatedOn = stockIssue.CreatedOn,
            CreatedBy = stockIssue.CreatedBy,
            Items = stockIssue.Items.Select(item => new StockIssueItemDto
            {
                Id = item.Id,
                StockIssueId = item.StockIssueId,
                ProductId = item.ProductId,
                ProductName = products[item.ProductId].Name,
                ProductSku = products[item.ProductId].Sku,
                UomId = products[item.ProductId].BaseUomId,
                UomName = products[item.ProductId].UomName,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}
