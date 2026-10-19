using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturns;

public class GetPurchaseReturnsQueryHandler : IRequestHandler<GetPurchaseReturnsQuery, PagedResult<PurchaseReturnDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetPurchaseReturnsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<PagedResult<PurchaseReturnDto>> Handle(GetPurchaseReturnsQuery request, CancellationToken cancellationToken)
    {
        if (request.StartDate.HasValue && request.EndDate.HasValue
            && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new InvalidOperationException("Start Date cannot be after End Date.");
        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
            throw new InvalidOperationException("Invalid purchase return status.");

        var companyId = _currentUserService.CompanyId;
        IQueryable<PurchaseReturn> query = _context.PurchaseReturns
            .AsNoTracking()
            .Where(pr => pr.CompanyId == companyId && !pr.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var keyword = request.Q.Trim().ToLower();
            query = query.Where(pr => pr.ReturnNo.ToLower().Contains(keyword)
                || (pr.Supplier != null && pr.Supplier.CompanyName.ToLower().Contains(keyword)));
        }
        if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
            query = query.Where(pr => pr.SupplierId == request.SupplierId.Value);
        if (request.Status.HasValue)
            query = query.Where(pr => pr.Status == request.Status.Value);
        if (request.StartDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            query = query.Where(pr => pr.ReturnDate >= start);
        }
        if (request.EndDate.HasValue)
        {
            var end = request.EndDate.Value.Date;
            query = query.Where(pr => pr.ReturnDate.Date <= end);
        }

        var totalRecords = await query.CountAsync(cancellationToken);
        var sortedQuery = request.SortField?.Trim().ToLowerInvariant() switch
        {
            "returnno" => request.Order == 1 ? query.OrderBy(pr => pr.ReturnNo) : query.OrderByDescending(pr => pr.ReturnNo),
            "suppliername" => request.Order == 1 ? query.OrderBy(pr => pr.Supplier!.CompanyName) : query.OrderByDescending(pr => pr.Supplier!.CompanyName),
            "status" => request.Order == 1 ? query.OrderBy(pr => pr.Status) : query.OrderByDescending(pr => pr.Status),
            "createdon" => request.Order == 1 ? query.OrderBy(pr => pr.CreatedOn) : query.OrderByDescending(pr => pr.CreatedOn),
            "postedon" => request.Order == 1 ? query.OrderBy(pr => pr.PostedOn) : query.OrderByDescending(pr => pr.PostedOn),
            _ => request.Order == 1 ? query.OrderBy(pr => pr.ReturnDate) : query.OrderByDescending(pr => pr.ReturnDate)
        };

        query = sortedQuery.ThenBy(pr => pr.Id);

        if (request.PageNumber.HasValue && request.PageSize.HasValue)
        {
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
                .Take(request.PageSize.Value);
        }

        var items = await query
            .Select(pr => new PurchaseReturnDto
            {
                Id = pr.Id,
                ReturnNo = pr.ReturnNo,
                SupplierId = pr.SupplierId,
                SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
                ReturnDate = pr.ReturnDate,
                Status = pr.Status,
                ReturnReason = pr.ReturnReason,
                PostedBy = pr.PostedBy,
                PostedOn = pr.PostedOn,
                CreatedOn = pr.CreatedOn,
                CreatedBy = pr.CreatedBy,
                Items = pr.Items
                    .Where(item => item.CompanyId == companyId && !item.DeletedOn.HasValue)
                    .OrderBy(item => item.Id)
                    .Select(item => new PurchaseReturnItemDto
                    {
                        Id = item.Id,
                        PurchaseReturnId = item.PurchaseReturnId,
                        GoodReceiptItemId = item.GoodReceiptItemId,
                        GoodReceiptId = item.GoodReceiptItem!.GoodReceiptId,
                        ReceiptNo = item.GoodReceiptItem.GoodReceipt.ReceiptNo,
                        PurchaseOrderId = item.GoodReceiptItem.GoodReceipt.PurchaseOrderId,
                        PurchaseOrderNo = item.GoodReceiptItem.GoodReceipt.PurchaseOrder != null
                            ? item.GoodReceiptItem.GoodReceipt.PurchaseOrder.PurchaseOrderNo : string.Empty,
                        ProductId = item.GoodReceiptItem.ProductId,
                        ProductName = item.GoodReceiptItem.Product.Name,
                        UomId = item.GoodReceiptItem.UomId,
                        UomName = item.GoodReceiptItem.Uom.Name,
                        WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId,
                        WarehouseName = item.GoodReceiptItem.GoodReceipt.Warehouse != null
                            ? item.GoodReceiptItem.GoodReceipt.Warehouse.Name : string.Empty,
                        ReturnQuantity = item.ReturnQuantity
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        var posterIds = items.Where(item => !string.IsNullOrWhiteSpace(item.PostedBy))
            .Select(item => item.PostedBy!).Distinct().ToArray();
        if (posterIds.Length > 0)
        {
            var names = await _identityService.GetUserNamesAsync(posterIds, cancellationToken);
            foreach (var item in items.Where(item => !string.IsNullOrWhiteSpace(item.PostedBy)))
                item.PostedByName = names.TryGetValue(item.PostedBy!, out var name) ? name : "Unknown user";
        }

        return new PagedResult<PurchaseReturnDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalRecords
        };
    }
}
