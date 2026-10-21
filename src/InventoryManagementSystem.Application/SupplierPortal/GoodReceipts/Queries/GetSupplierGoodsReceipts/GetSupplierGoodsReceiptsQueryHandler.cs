using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.GoodReceipts.Queries.GetSupplierGoodsReceipts;

public class GetSupplierGoodsReceiptsQueryHandler : IRequestHandler<GetSupplierGoodsReceiptsQuery, PagedResult<SupplierDocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierGoodsReceiptsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SupplierDocumentDto>> Handle(GetSupplierGoodsReceiptsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new ArgumentException("Start date must not be after end date.");

        var query = _context.GoodReceipts.AsNoTracking()
            .Where(gr => gr.SupplierId == supplierId && !gr.DeletedOn.HasValue && gr.Status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(gr => gr.ReceiptNo.Contains(keyword));
        }
        if (request.StartDate.HasValue)
            query = query.Where(gr => gr.ReceiptDate.Date >= request.StartDate.Value.Date);
        if (request.EndDate.HasValue)
            query = query.Where(gr => gr.ReceiptDate.Date <= request.EndDate.Value.Date);

        var totalRecords = await query.CountAsync(cancellationToken);
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var orderedQuery = request.SortField?.ToLower() switch
        {
            "documentno" => request.Order == 1 ? query.OrderBy(gr => gr.ReceiptNo) : query.OrderByDescending(gr => gr.ReceiptNo),
            "documentdate" => request.Order == 1 ? query.OrderBy(gr => gr.ReceiptDate) : query.OrderByDescending(gr => gr.ReceiptDate),
            "totalamount" => request.Order == 1 ? query.OrderBy(gr => gr.TotalAmount) : query.OrderByDescending(gr => gr.TotalAmount),
            _ => query.OrderByDescending(gr => gr.CreatedOn ?? gr.ReceiptDate)
        };
        var items = await orderedQuery.ThenByDescending(gr => gr.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(gr => new SupplierDocumentDto
            {
                Id = gr.Id,
                DocumentNo = gr.ReceiptNo,
                DocumentDate = gr.ReceiptDate,
                Status = "Received",
                WarehouseName = gr.Warehouse != null ? gr.Warehouse.Name : string.Empty,
                PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.PurchaseOrderNo : null,
                SupplierName = gr.Supplier != null ? gr.Supplier.CompanyName : string.Empty,
                SupplierAddress = gr.Supplier != null ? gr.Supplier.Address : null,
                TotalItems = gr.Items.Count(item => !item.DeletedOn.HasValue),
                TotalAmount = gr.TotalAmount,
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
