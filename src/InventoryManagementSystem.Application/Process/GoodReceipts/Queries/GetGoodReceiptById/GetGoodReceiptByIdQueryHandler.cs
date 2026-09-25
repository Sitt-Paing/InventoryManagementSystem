using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceiptById;

public class GetGoodReceiptByIdQueryHandler : IRequestHandler<GetGoodReceiptByIdQuery, GoodReceiptDto?>
{
    private readonly IApplicationDbContext _context;

    public GetGoodReceiptByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GoodReceiptDto?> Handle(GetGoodReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.GoodReceipts
            .AsNoTracking()
            .Where(gr => gr.Id == request.Id && !gr.DeletedOn.HasValue)
            .Select(gr => new GoodReceiptDto
            {
                Id = gr.Id,
                ReceiptNo = gr.ReceiptNo,
                WarehouseId = gr.WarehouseId,
                WarehouseName = gr.Warehouse != null ? gr.Warehouse.Name : string.Empty,
                PurchaseOrderId = gr.PurchaseOrderId,
                PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.PurchaseOrderNo : string.Empty,
                SupplierId = gr.SupplierId,
                SupplierName = gr.Supplier != null ? gr.Supplier.CompanyName : string.Empty,
                SupplierCode = gr.Supplier != null ? gr.Supplier.SupplierCode : null,
                ReceiptDate = gr.ReceiptDate,
                Status = gr.Status,
                ReceivedBy = gr.ReceivedBy,
                Note = gr.Note,
                CreatedOn = gr.CreatedOn,
                CreatedBy = gr.CreatedBy,
                UpdatedOn = gr.UpdatedOn,
                UpdatedBy = gr.UpdatedBy,
                Items = gr.Items
                    .Where(i => !i.DeletedOn.HasValue)
                    .Select(i => new GoodReceiptItemDto
                    {
                        Id = i.Id,
                        GoodReceiptId = i.GoodReceiptId,
                        PurchaseOrderItemId = i.PurchaseOrderItemId,
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : string.Empty,
                        ProductSku = i.Product != null ? i.Product.Sku : null,
                        UomId = i.UomId,
                        UomName = i.Uom != null ? i.Uom.Name : string.Empty,
                        ReceivedQuantity = i.ReceivedQuantity,
                        CreatedOn = i.CreatedOn,
                        CreatedBy = i.CreatedBy
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
