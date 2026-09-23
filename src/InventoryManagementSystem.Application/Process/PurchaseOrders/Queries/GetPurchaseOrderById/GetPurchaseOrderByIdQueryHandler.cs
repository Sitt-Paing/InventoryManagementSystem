using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderById;

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto?> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .Include(p => p.Items)
                .ThenInclude(i => i.Uom)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.DeletedOn.HasValue, cancellationToken);

        if (po == null) return null;

        return new PurchaseOrderDto
        {
            Id = po.Id,
            PurchaseOrderNo = po.PurchaseOrderNo,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier != null ? po.Supplier.CompanyName : string.Empty,
            SupplierCode = po.Supplier != null ? po.Supplier.SupplierCode : null,
            WarehouseId = po.WarehouseId,
            WarehouseName = po.Warehouse != null ? po.Warehouse.Name : string.Empty,
            OrderDate = po.OrderDate,
            ExpectedDate = po.ExpectedDate,
            Status = po.Status,
            TotalAmount = po.TotalAmount,
            CreatedOn = po.CreatedOn,
            CreatedBy = po.CreatedBy,
            UpdatedOn = po.UpdatedOn,
            UpdatedBy = po.UpdatedBy,
            Items = po.Items
                .Where(i => !i.DeletedOn.HasValue)
                .Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    PurchaseOrderId = i.PurchaseOrderId,
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    ProductSku = i.Product != null ? i.Product.Sku : null,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    UomId = i.UomId,
                    UomName = i.Uom != null ? i.Uom.Name : string.Empty,
                    ReceivedQuantity = i.ReceivedQuantity,
                    CreatedOn = i.CreatedOn,
                    CreatedBy = i.CreatedBy,
                    UpdatedOn = i.UpdatedOn,
                    UpdatedBy = i.UpdatedBy
                }).ToList()
        };
    }
}
