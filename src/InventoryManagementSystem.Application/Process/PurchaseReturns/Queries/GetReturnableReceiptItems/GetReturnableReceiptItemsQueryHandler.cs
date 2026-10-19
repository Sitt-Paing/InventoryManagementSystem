using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetReturnableReceiptItems;

public class GetReturnableReceiptItemsQueryHandler
    : IRequestHandler<GetReturnableReceiptItemsQuery, PagedResult<ReturnableReceiptItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetReturnableReceiptItemsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<ReturnableReceiptItemDto>> Handle(GetReturnableReceiptItemsQuery request, CancellationToken cancellationToken)
    {
        if (request.SupplierId <= 0)
            throw new InvalidOperationException("A supplier must be selected.");
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new InvalidOperationException("Start Date cannot be after End Date.");

        var companyId = _currentUserService.CompanyId;
        var source = _context.GoodReceiptItems.AsNoTracking()
            .Where(item => item.CompanyId == companyId && !item.DeletedOn.HasValue
                && item.GoodReceipt.CompanyId == companyId && !item.GoodReceipt.DeletedOn.HasValue
                && item.GoodReceipt.Status && item.GoodReceipt.SupplierId == request.SupplierId
                && item.ReceivedQuantity > 0 && item.ReceivedBaseQuantity.HasValue && item.ReceivedBaseQuantity > 0
                && !item.Product.DeletedOn.HasValue
                && item.GoodReceipt.Warehouse != null && !item.GoodReceipt.Warehouse.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var keyword = request.Q.Trim().ToLower();
            source = source.Where(item => item.GoodReceipt.ReceiptNo.ToLower().Contains(keyword)
                || item.Product.Name.ToLower().Contains(keyword)
                || (item.GoodReceipt.PurchaseOrder != null && item.GoodReceipt.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(keyword)));
        }
        if (request.StartDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            source = source.Where(item => item.GoodReceipt.ReceiptDate >= start);
        }
        if (request.EndDate.HasValue)
        {
            var end = request.EndDate.Value.Date;
            source = source.Where(item => item.GoodReceipt.ReceiptDate.Date <= end);
        }

        var query = source.Select(item => new
        {
            Item = item,
            ReturnedQuantity = _context.PurchaseReturnItems
                .Where(returnItem => returnItem.GoodReceiptItemId == item.Id && returnItem.CompanyId == companyId
                    && !returnItem.DeletedOn.HasValue && returnItem.PurchaseReturn!.CompanyId == companyId
                    && !returnItem.PurchaseReturn.DeletedOn.HasValue && returnItem.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
                .Sum(returnItem => (decimal?)returnItem.ReturnQuantity) ?? 0m
        })
            .Where(row => row.Item.ReceivedQuantity > row.ReturnedQuantity);

        var totalRecords = await query.CountAsync(cancellationToken);
        query = query.OrderByDescending(row => row.Item.GoodReceipt.ReceiptDate).ThenBy(row => row.Item.Id);
        if (request.PageNumber.HasValue && request.PageSize.HasValue)
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value).Take(request.PageSize.Value);

        var items = await query.Select(row => new ReturnableReceiptItemDto
        {
            GoodReceiptItemId = row.Item.Id,
            GoodReceiptId = row.Item.GoodReceiptId,
            ReceiptNo = row.Item.GoodReceipt.ReceiptNo,
            ReceiptDate = row.Item.GoodReceipt.ReceiptDate,
            PurchaseOrderNo = row.Item.GoodReceipt.PurchaseOrder != null ? row.Item.GoodReceipt.PurchaseOrder.PurchaseOrderNo : string.Empty,
            ProductId = row.Item.ProductId,
            ProductName = row.Item.Product.Name,
            UomName = row.Item.Uom.Name,
            WarehouseId = row.Item.GoodReceipt.WarehouseId,
            WarehouseName = row.Item.GoodReceipt.Warehouse!.Name,
            ReceivedQuantity = row.Item.ReceivedQuantity,
            ReturnedQuantity = row.ReturnedQuantity,
            ReturnableQuantity = row.Item.ReceivedQuantity - row.ReturnedQuantity
        }).ToListAsync(cancellationToken);

        return new PagedResult<ReturnableReceiptItemDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? totalRecords
        };
    }
}
