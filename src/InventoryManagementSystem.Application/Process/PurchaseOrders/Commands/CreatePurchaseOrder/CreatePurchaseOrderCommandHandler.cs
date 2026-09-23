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

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _context;

    public CreatePurchaseOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNo = request.PurchaseOrderNo.Trim(),
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            OrderDate = request.OrderDate,
            ExpectedDate = request.ExpectedDate,
            Status = request.Status,
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

        foreach (var item in request.Items)
        {
            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = purchaseOrder.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UomId = item.UomId,
                ReceivedQuantity = item.ReceivedQuantity
            });
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);

        // Fetch supplier & warehouse details for return DTO
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
            Items = purchaseOrder.Items.Select(i => new PurchaseOrderItemDto
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
                CreatedBy = i.CreatedBy
            }).ToList()
        };
    }
}
