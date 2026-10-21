using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.GoodReceipts.Queries.GetSupplierGoodReceiptById;

public record GetSupplierGoodReceiptByIdQuery(Guid Id) : IRequest<SupplierDocumentDto?>;

public class GetSupplierGoodReceiptByIdQueryHandler : IRequestHandler<GetSupplierGoodReceiptByIdQuery, SupplierDocumentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierGoodReceiptByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SupplierDocumentDto?> Handle(GetSupplierGoodReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        var query = _context.GoodReceipts.AsNoTracking()
            .Where(gr => gr.SupplierId == supplierId && !gr.DeletedOn.HasValue && gr.Status);

        return await query.Where(gr => gr.Id == request.Id)
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
                Items = gr.Items.Where(item => !item.DeletedOn.HasValue).OrderBy(item => item.Id)
                    .Select(item => new SupplierDocumentItemDto
                    {
                        ProductName = item.Product.Name,
                        ProductSku = item.Product.Sku,
                        UomName = item.Uom.Name,
                        Quantity = item.ReceivedQuantity,
                        UnitPrice = item.UnitPrice,
                        LineAmount = item.LineAmount,
                        WarehouseName = gr.Warehouse != null ? gr.Warehouse.Name : string.Empty,
                        PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.PurchaseOrderNo : null,
                    }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
