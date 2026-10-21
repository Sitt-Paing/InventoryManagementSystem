using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.PurchaseOrders.Queries.GetSupplierPurchaseOrders;

public class GetSupplierPurchaseOrdersQueryHandler : IRequestHandler<GetSupplierPurchaseOrdersQuery, PagedResult<SupplierDocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierPurchaseOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SupplierDocumentDto>> Handle(GetSupplierPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new ArgumentException("Start date must not be after end date.");

        var query = _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.SupplierId == supplierId && !po.DeletedOn.HasValue && po.ApprovalStatus == PurchaseOrderApprovalStatus.Approved);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(po => po.PurchaseOrderNo.Contains(keyword));
        }
        if (request.StartDate.HasValue)
            query = query.Where(po => po.OrderDate.Date >= request.StartDate.Value.Date);
        if (request.EndDate.HasValue)
            query = query.Where(po => po.OrderDate.Date <= request.EndDate.Value.Date);

        var totalRecords = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var orderedQuery = request.SortField?.ToLower() switch
        {
            "documentno" => request.Order == 1 ? query.OrderBy(po => po.PurchaseOrderNo) : query.OrderByDescending(po => po.PurchaseOrderNo),
            "documentdate" => request.Order == 1 ? query.OrderBy(po => po.OrderDate) : query.OrderByDescending(po => po.OrderDate),
            "totalamount" => request.Order == 1 ? query.OrderBy(po => po.TotalAmount) : query.OrderByDescending(po => po.TotalAmount),
            _ => query.OrderByDescending(po => po.CreatedOn ?? po.OrderDate)
        };
        var items = await orderedQuery.ThenByDescending(po => po.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new SupplierDocumentDto
            {
                Id = po.Id,
                DocumentNo = po.PurchaseOrderNo,
                DocumentDate = po.OrderDate,
                ExpectedDate = po.ExpectedDate,
                Status = po.Status == PurchaseOrderStatus.Cancelled ? "Cancelled"
                    : po.Status == PurchaseOrderStatus.Completed ? "Completed"
                    : po.Status == PurchaseOrderStatus.PartiallyReceived ? "Partially Received" : "Pending",
                WarehouseName = po.Warehouse != null ? po.Warehouse.Name : string.Empty,
                Note = po.CancellationReason,
                SupplierName = po.Supplier != null ? po.Supplier.CompanyName : string.Empty,
                SupplierAddress = po.Supplier != null ? po.Supplier.Address : null,
                TotalItems = po.Items.Count(item => !item.DeletedOn.HasValue),
                TotalAmount = po.TotalAmount,
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
