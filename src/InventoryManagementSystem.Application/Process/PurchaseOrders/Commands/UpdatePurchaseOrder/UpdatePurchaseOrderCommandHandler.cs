using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;

public class UpdatePurchaseOrderCommandHandler : IRequestHandler<UpdatePurchaseOrderCommand, PurchaseOrderDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdatePurchaseOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto?> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null) return null;

        purchaseOrder.PurchaseOrderNo = request.PurchaseOrderNo.Trim();
        purchaseOrder.SupplierId = request.SupplierId;
        purchaseOrder.WarehouseId = request.WarehouseId;
        purchaseOrder.OrderDate = request.OrderDate;
        purchaseOrder.ExpectedDate = request.ExpectedDate;
        purchaseOrder.Status = request.Status;
        purchaseOrder.TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        // Update items (sync line items)
        var inputItemIds = request.Items
            .Where(i => i.Id.HasValue && i.Id.Value > 0)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        // Mark items not in payload as deleted or remove
        foreach (var existingItem in purchaseOrder.Items.Where(i => !i.DeletedOn.HasValue).ToList())
        {
            if (!inputItemIds.Contains(existingItem.Id))
            {
                existingItem.DeletedOn = DateTime.UtcNow;
            }
        }

        // Add or update items
        foreach (var inputItem in request.Items)
        {
            if (inputItem.Id.HasValue && inputItem.Id.Value > 0)
            {
                var existing = purchaseOrder.Items.FirstOrDefault(i => i.Id == inputItem.Id.Value);
                if (existing != null)
                {
                    existing.ProductId = inputItem.ProductId;
                    existing.Quantity = inputItem.Quantity;
                    existing.UnitPrice = inputItem.UnitPrice;
                    existing.UomId = inputItem.UomId;
                    existing.ReceivedQuantity = inputItem.ReceivedQuantity;
                    existing.DeletedOn = null;
                }
            }
            else
            {
                purchaseOrder.Items.Add(new PurchaseOrderItem
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    ProductId = inputItem.ProductId,
                    Quantity = inputItem.Quantity,
                    UnitPrice = inputItem.UnitPrice,
                    UomId = inputItem.UomId,
                    ReceivedQuantity = inputItem.ReceivedQuantity
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Fetch supplier & warehouse details
        var supplier = await _context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == purchaseOrder.SupplierId, cancellationToken);
        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == purchaseOrder.WarehouseId, cancellationToken);

        var productIds = purchaseOrder.Items.Select(i => i.ProductId).ToList();
        var uomIds = purchaseOrder.Items.Select(i => i.UomId).ToList();

        var products = await _context.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var uoms = await _context.UnitOfMeasures.AsNoTracking()
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        return new PurchaseOrderDto
        {
            Id = purchaseOrder.Id,
            PurchaseOrderNo = purchaseOrder.PurchaseOrderNo,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = supplier?.CompanyName ?? string.Empty,
            SupplierCode = supplier?.SupplierCode,
            WarehouseId = purchaseOrder.WarehouseId,
            WarehouseName = warehouse?.Name ?? string.Empty,
            OrderDate = purchaseOrder.OrderDate,
            ExpectedDate = purchaseOrder.ExpectedDate,
            Status = purchaseOrder.Status,
            TotalAmount = purchaseOrder.TotalAmount,
            CreatedOn = purchaseOrder.CreatedOn,
            CreatedBy = purchaseOrder.CreatedBy,
            UpdatedOn = purchaseOrder.UpdatedOn,
            UpdatedBy = purchaseOrder.UpdatedBy,
            Items = purchaseOrder.Items
                .Where(i => !i.DeletedOn.HasValue)
                .Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    PurchaseOrderId = i.PurchaseOrderId,
                    ProductId = i.ProductId,
                    ProductName = products.TryGetValue(i.ProductId, out var prod) ? prod.Name : string.Empty,
                    ProductSku = products.TryGetValue(i.ProductId, out prod) ? prod.Sku : null,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    UomId = i.UomId,
                    UomName = uoms.TryGetValue(i.UomId, out var uom) ? uom.Name : string.Empty,
                    ReceivedQuantity = i.ReceivedQuantity,
                    CreatedOn = i.CreatedOn,
                    CreatedBy = i.CreatedBy,
                    UpdatedOn = i.UpdatedOn,
                    UpdatedBy = i.UpdatedBy
                }).ToList()
        };
    }
}
