using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;

public record GetPurchaseReturnByIdQuery(Guid Id) : IRequest<PurchaseReturnDto>;

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
        var companyId = _currentUserService.CompanyId;
        var result = await _context.PurchaseReturns
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
