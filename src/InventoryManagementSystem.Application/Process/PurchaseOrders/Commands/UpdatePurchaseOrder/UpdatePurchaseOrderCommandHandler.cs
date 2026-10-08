using InventoryManagementSystem.Domain.Enums;
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
        await using var databaseTransaction = await _context.BeginTransactionAsync(cancellationToken);

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id && !p.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null) return null;

        // Validate identities before changing the tracked order or its items.
        var activeItemIds = purchaseOrder.Items
            .Where(i => !i.DeletedOn.HasValue)
            .Select(i => i.Id)
            .ToHashSet();
        var inputItemIds = new HashSet<long>();
        foreach (var item in request.Items)
        {
            // Null or zero identifies a new line; negative IDs are invalid.
            if (!item.Id.HasValue || item.Id.Value == 0) continue;

            if (item.Id.Value < 0 || !activeItemIds.Contains(item.Id.Value))
            {
                throw new InvalidOperationException(
                    "Every existing item must belong to this purchase order and must not be deleted.");
            }

            if (!inputItemIds.Add(item.Id.Value))
            {
                throw new InvalidOperationException(
                    "The same purchase order item cannot appear more than once.");
            }
        }

        if (request.SupplierId != purchaseOrder.SupplierId)
        {
            var hasActiveReceipts = await _context.GoodReceipts
                .AnyAsync(
                    gr => gr.PurchaseOrderId == purchaseOrder.Id
                        && !gr.DeletedOn.HasValue,
                    cancellationToken);

            if (hasActiveReceipts)
            {
                throw new InvalidOperationException(
                    "Cannot change the supplier of a purchase order with active goods receipts.");
            }
        }

        purchaseOrder.PurchaseOrderNo = request.PurchaseOrderNo.Trim();
        purchaseOrder.SupplierId = request.SupplierId;
        purchaseOrder.WarehouseId = request.WarehouseId;
        purchaseOrder.OrderDate = request.OrderDate;
        purchaseOrder.ExpectedDate = request.ExpectedDate;

        // Mark items not in payload as deleted or remove
        foreach (var existingItem in purchaseOrder.Items.Where(i => !i.DeletedOn.HasValue).ToList())
        {
            if (!inputItemIds.Contains(existingItem.Id))
            {
                if(existingItem.ReceivedQuantity > 0)
                {
                    throw new InvalidOperationException("Cannot remove a purchase order item that has received goods.");
                }
                existingItem.DeletedOn = DateTime.UtcNow;
            }
        }

        // Add or update items
        foreach (var inputItem in request.Items)
        {
            if (inputItem.Id.HasValue && inputItem.Id.Value > 0)
            {
                var existing = purchaseOrder.Items.Single(i => i.Id == inputItem.Id.Value && !i.DeletedOn.HasValue);
                if (existing != null)
                {
                    if(existing.ReceivedQuantity > 0 && (existing.ProductId != inputItem.ProductId || existing.UomId != inputItem.UomId))
                    {
                        throw new InvalidOperationException("Cannot change the product or UOM of a purchase order item that has received goods.");
                    }

                    if (inputItem.Quantity < existing.ReceivedQuantity)
                    {
                        throw new InvalidOperationException($"Ordered quantity cannot be less than the received quantity ({existing.ReceivedQuantity}).");
                    }

                    existing.ProductId = inputItem.ProductId;
                    existing.Quantity = inputItem.Quantity;
                    existing.UnitPrice = inputItem.UnitPrice;
                    existing.UomId = inputItem.UomId;
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
                    ReceivedQuantity = 0
                });
            }
        }

        purchaseOrder.TotalAmount = purchaseOrder.Items
            .Where(i => !i.DeletedOn.HasValue)
            .Sum(i => i.Quantity * i.UnitPrice);

        if(purchaseOrder.Status != PurchaseOrderStatus.Cancelled)
        {
            var activeItems = purchaseOrder.Items.Where(i => !i.DeletedOn.HasValue).ToList();
            if(activeItems.Count > 0 && activeItems.All(i => i.ReceivedQuantity >= i.Quantity))
            {
                purchaseOrder.Status = PurchaseOrderStatus.Completed;
            } 
            else if (activeItems.Any(i => i.ReceivedQuantity > 0))
            {
                purchaseOrder.Status = PurchaseOrderStatus.PartiallyReceived;
            }
            else
            {
                purchaseOrder.Status = PurchaseOrderStatus.Pending;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await databaseTransaction.CommitAsync(cancellationToken);

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
