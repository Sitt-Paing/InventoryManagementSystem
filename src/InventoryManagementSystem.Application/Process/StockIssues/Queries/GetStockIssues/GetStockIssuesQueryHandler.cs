using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssues;

public class GetStockIssuesQueryHandler : IRequestHandler<GetStockIssuesQuery, PagedResult<StockIssueDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetStockIssuesQueryHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<PagedResult<StockIssueDto>> Handle(GetStockIssuesQuery request, CancellationToken cancellationToken)
    {
        if (request.StartDate.HasValue && request.EndDate.HasValue
            && request.StartDate.Value.Date > request.EndDate.Value.Date)
        {
            throw new InvalidOperationException("Start Date cannot be after End Date.");
        }

        var query = _context.StockIssues
            .AsNoTracking()
            .Where(issue => !issue.DeletedOn.HasValue);

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(issue => issue.WarehouseId == request.WarehouseId.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(issue => issue.DepartmentId == request.DepartmentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(issue => issue.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(issue => issue.IssueNo.Contains(search)
                || issue.Department!.DeptName.Contains(search) || issue.ReceivedBy.Contains(search));
        }

        if (request.StartDate.HasValue)
        {
            var startDate = request.StartDate.Value.Date;
            query = query.Where(issue => issue.IssueDate >= startDate);
        }

        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.Date;
            query = query.Where(issue => issue.IssueDate.Date <= endDate);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        query = query.OrderByDescending(issue => issue.CreatedOn).ThenByDescending(issue => issue.Id);

        if (request.PageNumber.HasValue && request.PageSize.HasValue)
        {
            if (request.PageNumber.Value < 1 || request.PageSize.Value < 1)
                throw new InvalidOperationException("Page number and page size must be greater than 0.");
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                .Take(request.PageSize.Value);
        }

        var stockIssues = await query
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
                Items = issue.Items
                    .Where(item => !item.DeletedOn.HasValue)
                    .OrderBy(item => item.Id)
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
            })
            .ToListAsync(cancellationToken);

        var posterIds = stockIssues.Where(issue => !string.IsNullOrWhiteSpace(issue.PostedBy))
            .Select(item => item.PostedBy!).Distinct().ToArray();
        if (posterIds.Length > 0)
        {
            var names = await _identityService.GetUserNamesAsync(posterIds, cancellationToken);
            foreach (var issue in stockIssues.Where(issue => !string.IsNullOrWhiteSpace(issue.PostedBy)))
            {
                issue.PostedByName = names.TryGetValue(issue.PostedBy!, out var name) ? name : "Unknown user";
            }
        }

        return new PagedResult<StockIssueDto>
        {
            Items = stockIssues,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalRecords
        };
    }
}
