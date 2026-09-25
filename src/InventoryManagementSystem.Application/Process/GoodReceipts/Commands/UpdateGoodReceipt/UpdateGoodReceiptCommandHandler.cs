using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.UpdateGoodReceipt;

public class UpdateGoodReceiptCommandHandler : IRequestHandler<UpdateGoodReceiptCommand, GoodReceiptDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateGoodReceiptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GoodReceiptDto?> Handle(UpdateGoodReceiptCommand request, CancellationToken cancellationToken)
    {
        var goodReceipt = await _context.GoodReceipts
            .Include(gr => gr.Items)
            .FirstOrDefaultAsync(gr => gr.Id == request.Id && !gr.DeletedOn.HasValue, cancellationToken);

        if (goodReceipt == null)
        {
            return null;
        }

        goodReceipt.ReceiptDate = request.ReceiptDate;
        goodReceipt.Status = request.Status;
        goodReceipt.ReceivedBy = request.ReceivedBy.Trim();
        goodReceipt.Note = request.Note?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var supplier = await _context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == goodReceipt.SupplierId, cancellationToken);
        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == goodReceipt.WarehouseId, cancellationToken);
        var po = await _context.PurchaseOrders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == goodReceipt.PurchaseOrderId, cancellationToken);

        var productIds = goodReceipt.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var uomIds = goodReceipt.Items.Select(i => i.UomId).Distinct().ToList();
        var uoms = await _context.UnitOfMeasures.AsNoTracking()
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        return new GoodReceiptDto
        {
            Id = goodReceipt.Id,
            ReceiptNo = goodReceipt.ReceiptNo,
            WarehouseId = goodReceipt.WarehouseId,
            WarehouseName = warehouse?.Name ?? string.Empty,
            PurchaseOrderId = goodReceipt.PurchaseOrderId,
            PurchaseOrderNo = po?.PurchaseOrderNo ?? string.Empty,
            SupplierId = goodReceipt.SupplierId,
            SupplierName = supplier?.CompanyName ?? string.Empty,
            SupplierCode = supplier?.SupplierCode,
            ReceiptDate = goodReceipt.ReceiptDate,
            Status = goodReceipt.Status,
            ReceivedBy = goodReceipt.ReceivedBy,
            Note = goodReceipt.Note,
            CreatedOn = goodReceipt.CreatedOn,
            CreatedBy = goodReceipt.CreatedBy,
            UpdatedOn = goodReceipt.UpdatedOn,
            UpdatedBy = goodReceipt.UpdatedBy,
            Items = goodReceipt.Items
                .Where(i => !i.DeletedOn.HasValue)
                .Select(i => new GoodReceiptItemDto
                {
                    Id = i.Id,
                    GoodReceiptId = i.GoodReceiptId,
                    PurchaseOrderItemId = i.PurchaseOrderItemId,
                    ProductId = i.ProductId,
                    ProductName = products.TryGetValue(i.ProductId, out var p) ? p.Name : string.Empty,
                    ProductSku = products.TryGetValue(i.ProductId, out p) ? p.Sku : null,
                    UomId = i.UomId,
                    UomName = uoms.TryGetValue(i.UomId, out var u) ? u.Name : string.Empty,
                    ReceivedQuantity = i.ReceivedQuantity,
                    CreatedOn = i.CreatedOn,
                    CreatedBy = i.CreatedBy
                }).ToList()
        };
    }
}
