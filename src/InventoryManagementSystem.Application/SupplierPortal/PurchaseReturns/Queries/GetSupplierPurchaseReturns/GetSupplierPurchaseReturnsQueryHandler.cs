using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.PurchaseReturns.Queries.GetSupplierPurchaseReturns;

public class GetSupplierPurchaseReturnsQueryHandler : IRequestHandler<GetSupplierPurchaseReturnsQuery, PagedResult<SupplierDocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierPurchaseReturnsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SupplierDocumentDto>> Handle(GetSupplierPurchaseReturnsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new ArgumentException("Start date must not be after end date.");

        var query = _context.PurchaseReturns.AsNoTracking()
            .Where(pr => pr.SupplierId == supplierId && !pr.DeletedOn.HasValue && pr.Status == PurchaseReturnStatus.Posted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(pr => pr.ReturnNo.Contains(keyword));
        }
        if (request.StartDate.HasValue)
            query = query.Where(pr => pr.ReturnDate.Date >= request.StartDate.Value.Date);
        if (request.EndDate.HasValue)
            query = query.Where(pr => pr.ReturnDate.Date <= request.EndDate.Value.Date);

        var totalRecords = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var orderedQuery = request.SortField?.ToLower() switch
        {
            "documentno" => request.Order == 1 ? query.OrderBy(pr => pr.ReturnNo) : query.OrderByDescending(pr => pr.ReturnNo),
            "documentdate" => request.Order == 1 ? query.OrderBy(pr => pr.ReturnDate) : query.OrderByDescending(pr => pr.ReturnDate),
            "totalamount" => request.Order == 1 ? query.OrderBy(pr => pr.TotalAmount) : query.OrderByDescending(pr => pr.TotalAmount),
            _ => query.OrderByDescending(pr => pr.CreatedOn ?? pr.ReturnDate)
        };
        var items = await orderedQuery.ThenByDescending(pr => pr.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(pr => new SupplierDocumentDto
            {
                Id = pr.Id,
                DocumentNo = pr.ReturnNo,
                DocumentDate = pr.ReturnDate,
                Status = "Posted",
                Note = pr.ReturnReason,
                SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
                SupplierAddress = pr.Supplier != null ? pr.Supplier.Address : null,
                TotalItems = pr.Items.Count(item => !item.DeletedOn.HasValue),
                TotalAmount = pr.TotalAmount,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDocumentDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
