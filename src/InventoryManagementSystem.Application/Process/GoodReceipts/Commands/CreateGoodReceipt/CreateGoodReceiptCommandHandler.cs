using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Commands.CreateGoodReceipt;

public class CreateGoodReceiptCommandHandler : IRequestHandler<CreateGoodReceiptCommand, GoodReceiptDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUomConversionService _uomConversionService;

    public CreateGoodReceiptCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IUomConversionService uomConversionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _uomConversionService = uomConversionService;
    }

    public async Task<GoodReceiptDto> Handle(CreateGoodReceiptCommand request, CancellationToken cancellationToken)
    {
        await using var databaseTransaction = await _context.BeginTransactionAsync(cancellationToken);

        var receiptNo = request.ReceiptNo.Trim();
        var exists = await _context.GoodReceipts
            .AnyAsync(r => r.ReceiptNo == receiptNo && !r.DeletedOn.HasValue, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Goods Receipt with number '{receiptNo}' already exists.");
        }

        var purchaseOrder = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId && !p.DeletedOn.HasValue, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new KeyNotFoundException($"Purchase Order with ID '{request.PurchaseOrderId}' was not found.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Completed)
        {
            throw new InvalidOperationException($"Purchase Order '{purchaseOrder.PurchaseOrderNo}' has already been fully received.");
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Purchase Order '{purchaseOrder.PurchaseOrderNo}' has been cancelled.");
        }

        if (request.SupplierId != purchaseOrder.SupplierId)
        {
            throw new InvalidOperationException("Supplier must match the selected Purchase Order.");
        }

        foreach (var itemGroup in request.Items.GroupBy(i => i.PurchaseOrderItemId))
        {
            var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == itemGroup.Key && !i.DeletedOn.HasValue);
            if (poItem == null)
            {
                throw new InvalidOperationException($"Purchase Order Item '{itemGroup.Key}' does not belong to the selected Purchase Order.");
            }

            if (itemGroup.Any(i => i.ProductId != poItem.ProductId || i.UomId != poItem.UomId))
            {
                throw new InvalidOperationException($"Product and UOM must match Purchase Order Item '{itemGroup.Key}'.");
            }

            if (itemGroup.Sum(i => i.ReceivedQuantity) > poItem.Quantity - poItem.ReceivedQuantity)
            {
                throw new InvalidOperationException($"Received quantity exceeds the remaining quantity for Purchase Order Item '{itemGroup.Key}'.");
            }
        }

        var effectiveUserId = _currentUserService.UserName ?? _currentUserService.UserId ?? "System";

        var goodReceipt = new GoodReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNo = receiptNo,
            WarehouseId = request.WarehouseId,
            PurchaseOrderId = request.PurchaseOrderId,
            SupplierId = request.SupplierId,
            ReceiptDate = request.ReceiptDate,
            Status = request.Status,
            ReceivedBy = request.ReceivedBy.Trim(),
            Note = request.Note?.Trim()
        };

        foreach (var item in request.Items)
        {
            var baseQuantity = await _uomConversionService.ConvertToBaseUomAsync(item.ProductId, item.UomId, item.ReceivedQuantity, cancellationToken);

            var receiptItem = new GoodReceiptItem
            {
                GoodReceiptId = goodReceipt.Id,
                PurchaseOrderItemId = item.PurchaseOrderItemId,
                ProductId = item.ProductId,
                UomId = item.UomId,
                ReceivedQuantity = item.ReceivedQuantity,
                ReceivedBaseQuantity = baseQuantity
            };

            goodReceipt.Items.Add(receiptItem);

            var random = new Random();

            _context.StockTransactions.Add(new StockTransaction
            {
                ProductId = item.ProductId,
                Quantity = baseQuantity,
                UserId = effectiveUserId,
                WarehouseId = request.WarehouseId,
                WarehouseLocationId = 0,
                ReferenceNo = random.Next(100000, 1000000),
                TransactionType = "IN",
                TransactionDate = request.ReceiptDate,
                GoodReceipt = goodReceipt,
                GoodReceiptItem = receiptItem,
                Note = $"Goods Receipt: {receiptNo}"
            });

            // Update PurchaseOrderItem received quantity
            var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == item.PurchaseOrderItemId);
            if (poItem != null)
            {
                poItem.ReceivedQuantity += item.ReceivedQuantity;
            }

            // Update Warehouse stock
            var stock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.WarehouseId == request.WarehouseId && !s.DeletedOn.HasValue, cancellationToken);

            if (stock == null)
            {
                stock = new WarehouseStocks
                {
                    ProductId = item.ProductId,
                    WarehouseId = request.WarehouseId,
                    Quantity = 0,
                    CreatedOn = DateTime.Now,
                    CreatedBy = effectiveUserId
                };
                _context.WarehouseStocks.Add(stock);
            }


            stock.Quantity += baseQuantity;
        }

        // Update PO Status based on received quantities
        var activePoItems = purchaseOrder.Items.Where(i => !i.DeletedOn.HasValue).ToList();
        if (activePoItems.Count > 0 && activePoItems.All(i => i.ReceivedQuantity >= i.Quantity))
        {
            purchaseOrder.Status = PurchaseOrderStatus.Completed;
        }
        else if (activePoItems.Any(i => i.ReceivedQuantity > 0))
        {
            purchaseOrder.Status = PurchaseOrderStatus.PartiallyReceived;
        }
        else
        {
            purchaseOrder.Status = PurchaseOrderStatus.Pending;
        }

        _context.GoodReceipts.Add(goodReceipt);
        await _context.SaveChangesAsync(cancellationToken);

        // Sync Product CurrentStock to sum of all warehouse stocks
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CurrentStock = await _context.WarehouseStocks
                .Where(w => w.ProductId == product.Id && !w.DeletedOn.HasValue)
                .SumAsync(w => w.Quantity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        // Fetch display details for return DTO
        var supplier = await _context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == goodReceipt.SupplierId, cancellationToken);
        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == goodReceipt.WarehouseId, cancellationToken);

        var productDict = products.ToDictionary(p => p.Id, p => p);
        var uomIds = request.Items.Select(i => i.UomId).Distinct().ToList();
        var uomDict = await _context.UnitOfMeasures.AsNoTracking()
            .Where(u => uomIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        return new GoodReceiptDto
        {
            Id = goodReceipt.Id,
            ReceiptNo = goodReceipt.ReceiptNo,
            WarehouseId = goodReceipt.WarehouseId,
            WarehouseName = warehouse?.Name ?? string.Empty,
            PurchaseOrderId = goodReceipt.PurchaseOrderId,
            PurchaseOrderNo = purchaseOrder.PurchaseOrderNo,
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
            Items = goodReceipt.Items.Select(i => new GoodReceiptItemDto
            {
                Id = i.Id,
                GoodReceiptId = i.GoodReceiptId,
                PurchaseOrderItemId = i.PurchaseOrderItemId,
                ProductId = i.ProductId,
                ProductName = productDict.TryGetValue(i.ProductId, out var p) ? p.Name : string.Empty,
                ProductSku = productDict.TryGetValue(i.ProductId, out p) ? p.Sku : null,
                UomId = i.UomId,
                UomName = uomDict.TryGetValue(i.UomId, out var u) ? u.Name : string.Empty,
                ReceivedQuantity = i.ReceivedQuantity,
                CreatedOn = i.CreatedOn,
                CreatedBy = i.CreatedBy
            }).ToList()
        };
    }
}
