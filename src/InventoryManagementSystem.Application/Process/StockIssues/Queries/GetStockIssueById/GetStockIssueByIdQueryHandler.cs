using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssueById;

public class GetStockIssueByIdQueryHandler : IRequestHandler<GetStockIssueByIdQuery, StockIssueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetStockIssueByIdQueryHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<StockIssueDto> Handle(GetStockIssueByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _context.StockIssues
            .AsNoTracking()
            .Where(issue => issue.Id == request.Id && !issue.DeletedOn.HasValue)
            .Select(issue => new StockIssueDto
            {
                Id = issue.Id,
                IssueNo = issue.IssueNo,
                IssueDate = issue.IssueDate,
                WarehouseId = issue.WarehouseId,
                WarehouseName = issue.Warehouse != null ? issue.Warehouse.Name : string.Empty,
                DepartmentId = issue.DepartmentId,
                DepartmentName = issue.Department != null ? issue.Department.DeptName : string.Empty,
                ReceivedBy = issue.ReceivedBy,
                Note = issue.Note,
                Status = issue.Status,
                PostedBy = issue.PostedBy,
                PostedOn = issue.PostedOn,
                CreatedBy = issue.CreatedBy,
                CreatedOn = issue.CreatedOn,
                UpdatedOn = issue.UpdatedOn,
                UpdatedBy = issue.UpdatedBy,
                Items = issue.Items.Where(item => !item.DeletedOn.HasValue).OrderBy(item => item.Id)
                    .Select(item => new StockIssueItemDto
                    {
                        Id = item.Id,
                        StockIssueId = item.StockIssueId,
                        ProductId = item.ProductId,
                        ProductName = item.Product != null ? item.Product.Name : string.Empty,
                        ProductSku = item.Product != null ? item.Product.Sku : null,
                        UomId = item.Product != null ? item.Product.BaseUomId : 0,
                        UomName = item.Product != null ? item.Product.BaseUom.Name : string.Empty,
                        Quantity = item.Quantity
                    }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new KeyNotFoundException("Stock Issue was not found.");

        if (!string.IsNullOrWhiteSpace(result.PostedBy))
        {
            var names = await _identityService.GetUserNamesAsync(new[] { result.PostedBy }, cancellationToken);
            result.PostedByName = names.TryGetValue(result.PostedBy, out var name) ? name : "Unknown user";
        }

        return result;
    }
}
