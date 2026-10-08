using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderById;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreatePurchaseOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey == Guid.Empty)
            throw new InvalidOperationException("A non-empty idempotency key is required.");
        // Per-key transaction lock serializes retries without range-locking other new POs.
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken, System.Data.IsolationLevel.ReadCommitted);
        await _context.LockPurchaseOrderAsync(request.IdempotencyKey, cancellationToken);
        var companyId = _currentUserService.CompanyId
            ?? throw new InvalidOperationException("A company context is required to create a purchase order.");

        var requestHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            request.SupplierId, request.WarehouseId, request.OrderDate, request.ExpectedDate,
            Items = request.Items.OrderBy(i => i.ProductId).ThenBy(i => i.UomId)
                .ThenBy(i => i.Quantity).ThenBy(i => i.UnitPrice).ToArray()
        })));
        var previous = await _context.PurchaseOrders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.IdempotencyKey, cancellationToken);
        if (previous != null)
        {
            if (previous.CompanyId != companyId || previous.DeletedOn.HasValue || previous.CreateRequestHash != requestHash)
                throw new InvalidOperationException("This idempotency key was already used for another or deleted request.");
            var previousDto = await new GetPurchaseOrderByIdQueryHandler(_context)
                .Handle(new GetPurchaseOrderByIdQuery(previous.Id), cancellationToken)
                ?? throw new InvalidOperationException("The previously created purchase order is unavailable.");
            await transaction.CommitAsync(cancellationToken);
            return previousDto;
        }

        var validSupplier = await _context.Suppliers.AsNoTracking()
            .AnyAsync(s => s.Id == request.SupplierId
                && !s.DeletedOn.HasValue
                && s.Status
                && s.CompanyId == companyId, cancellationToken);

        if (!validSupplier)
        {
            throw new InvalidOperationException(
                "The supplier must exist, be active, and belong to the purchase order's company.");
        }

        var validWarehouse = await _context.Warehouses.AsNoTracking()
            .AnyAsync(w => w.Id == request.WarehouseId
                && !w.DeletedOn.HasValue
                && w.Status
                && w.CompanyId == companyId, cancellationToken);

        if (!validWarehouse)
        {
            throw new InvalidOperationException(
                "The warehouse must exist, be active, and belong to the purchase order's company.");
        }

        var sequenceNumber = await _context.NextPurchaseOrderNumberAsync(cancellationToken);
        var orderNumber = $"PO-{request.OrderDate:yyyy}-{sequenceNumber:D8}";
        var purchaseOrder = new PurchaseOrder
        {
            Id = request.IdempotencyKey,
            CompanyId = companyId,
            PurchaseOrderNo = orderNumber,
            CreateRequestHash = requestHash,
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            OrderDate = request.OrderDate,
            ExpectedDate = request.ExpectedDate,
            Status = PurchaseOrderStatus.Pending,
            TotalAmount = decimal.Round(request.Items.Sum(i => i.Quantity * i.UnitPrice), 2, MidpointRounding.AwayFromZero)
        };

        foreach (var item in request.Items)
        {
            var product = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.CompanyId == companyId
                    && !p.DeletedOn.HasValue && p.Status, cancellationToken)
                ?? throw new InvalidOperationException("The product must be active and belong to the PO's company.");
            var validUoms = await _context.UnitOfMeasures.AsNoTracking()
                .Where(u => (u.Id == item.UomId || u.Id == product.BaseUomId)
                    && u.CompanyId == companyId && !u.DeletedOn.HasValue && u.IsActive)
                .Select(u => u.Id).ToListAsync(cancellationToken);
            if (!validUoms.Contains(item.UomId) || !validUoms.Contains(product.BaseUomId))
                throw new InvalidOperationException("The ordered and base UOMs must be active and belong to the PO's company.");
            if (item.UomId != product.BaseUomId && !await _context.ProductUomConversions.AnyAsync(c =>
                c.ProductId == item.ProductId && c.FromUomId == item.UomId && c.ToUomId == product.BaseUomId
                && c.CompanyId == companyId && !c.DeletedOn.HasValue && c.IsActive && c.ConversionFactor > 0,
                cancellationToken))
                throw new InvalidOperationException("An active conversion to the product's base UOM is required.");
            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                CompanyId = companyId,
                PurchaseOrderId = purchaseOrder.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UomId = item.UomId,
                ReceivedQuantity = 0
            });
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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
