using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using MediatR;
using InventoryManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;

public class GetPurchaseReturnByIdQueryHandler : IRequestHandler<GetPurchaseReturnByIdQuery, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetPurchaseReturnByIdQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<PurchaseReturnDto> Handle(GetPurchaseReturnByIdQuery request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        PurchaseReturnDto? result = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(pr => pr.Id == request.Id && pr.CompanyId == companyId && !pr.DeletedOn.HasValue)
            .Select(pr => new PurchaseReturnDto
            {
                Id = pr.Id,
                ReturnNo = pr.ReturnNo,
                SupplierId = pr.SupplierId,
                SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
                ReturnDate = pr.ReturnDate,
                ReturnReason = pr.ReturnReason,
                Status = pr.Status,
                PostedBy = pr.PostedBy,
                PostedOn = pr.PostedOn,
                CreatedBy = pr.CreatedBy,
                CreatedOn = pr.CreatedOn,
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
                    ReceivedQuantity = item.GoodReceiptItem.ReceivedQuantity,
                    ReturnableQuantity = item.GoodReceiptItem.ReceivedQuantity -
                        (_context.PurchaseReturnItems.Where(returned => returned.CompanyId == companyId
                            && returned.GoodReceiptItemId == item.GoodReceiptItemId
                            && !returned.DeletedOn.HasValue
                            && returned.PurchaseReturn!.CompanyId == companyId
                            && !returned.PurchaseReturn.DeletedOn.HasValue
                            && returned.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
                            .Sum(returned => (decimal?)returned.ReturnQuantity) ?? 0),
                    ReturnQuantity = item.ReturnQuantity
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new KeyNotFoundException("Purchase Return was not found.");

        if (!string.IsNullOrWhiteSpace(result.PostedBy))
        {
            var names = await _identityService.GetUserNamesAsync(new[] { result.PostedBy }, cancellationToken);
            result.PostedByName = names.TryGetValue(result.PostedBy, out var name) ? name : "Unknown user";
        }

        return result;
    }
}
