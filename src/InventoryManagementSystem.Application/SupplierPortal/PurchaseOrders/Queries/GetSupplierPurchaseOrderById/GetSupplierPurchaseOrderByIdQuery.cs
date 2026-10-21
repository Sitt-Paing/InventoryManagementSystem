using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.PurchaseOrders.Queries.GetSupplierPurchaseOrderById;

public record GetSupplierPurchaseOrderByIdQuery(Guid Id) : IRequest<SupplierDocumentDto?>;

public class GetSupplierPurchaseOrderByIdQueryHandler : IRequestHandler<GetSupplierPurchaseOrderByIdQuery, SupplierDocumentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierPurchaseOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SupplierDocumentDto?> Handle(GetSupplierPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");

        var supplierId = _currentUser.SupplierId.Value;

        var query = _context.PurchaseOrders.AsNoTracking()
            .Where(po => po.SupplierId == supplierId && !po.DeletedOn.HasValue && po.ApprovalStatus == PurchaseOrderApprovalStatus.Approved);

        return await query.Where(po => po.Id == request.Id)
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
                Items = po.Items.Where(item => !item.DeletedOn.HasValue).OrderBy(item => item.Id)
                    .Select(item => new SupplierDocumentItemDto
                    {
                        ProductName = item.Product.Name,
                        ProductSku = item.Product.Sku,
                        UomName = item.Uom.Name,
                        Quantity = item.Quantity,
                        ReceivedQuantity = item.ReceivedQuantity,
                        UnitPrice = item.UnitPrice,
                        LineAmount = Math.Round(item.Quantity * item.UnitPrice, 2),
                        WarehouseName = po.Warehouse != null ? po.Warehouse.Name : string.Empty,
                    }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
