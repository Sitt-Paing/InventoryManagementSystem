using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.PurchaseReturns.Queries.GetSupplierPurchaseReturnById;

public record GetSupplierPurchaseReturnByIdQuery(Guid Id) : IRequest<SupplierDocumentDto?>;

public class GetSupplierPurchaseReturnByIdQueryHandler : IRequestHandler<GetSupplierPurchaseReturnByIdQuery, SupplierDocumentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierPurchaseReturnByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SupplierDocumentDto?> Handle(GetSupplierPurchaseReturnByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        var query = _context.PurchaseReturns.AsNoTracking()
            .Where(pr => pr.SupplierId == supplierId && !pr.DeletedOn.HasValue && pr.Status == PurchaseReturnStatus.Posted);

        return await query.Where(pr => pr.Id == request.Id)
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
                Items = pr.Items.Where(item => !item.DeletedOn.HasValue).OrderBy(item => item.Id)
                    .Select(item => new SupplierDocumentItemDto
                    {
                        ProductName = item.GoodReceiptItem!.Product.Name,
                        ProductSku = item.GoodReceiptItem.Product.Sku,
                        UomName = item.GoodReceiptItem.Uom.Name,
                        Quantity = item.ReturnQuantity,
                        UnitPrice = item.UnitPrice,
                        LineAmount = item.LineAmount,
                        WarehouseName = item.GoodReceiptItem.GoodReceipt.Warehouse != null
                            ? item.GoodReceiptItem.GoodReceipt.Warehouse.Name : string.Empty,
                        ReceiptNo = item.GoodReceiptItem.GoodReceipt.ReceiptNo,
                        PurchaseOrderNo = item.GoodReceiptItem.GoodReceipt.PurchaseOrder != null
                            ? item.GoodReceiptItem.GoodReceipt.PurchaseOrder.PurchaseOrderNo : null,
                    }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
