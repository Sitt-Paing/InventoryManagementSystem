using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.UpdateStockTransactions;

public class UpdateStockTransactionsCommandHandler : IRequestHandler<UpdateStockTransactionsCommand, StockTransactionsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateStockTransactionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<StockTransactionsDto?> Handle(UpdateStockTransactionsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.StockTransactions
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.DeletedOn.HasValue, cancellationToken);

        if (transaction == null)
        {
            return null;
        }

        var effectiveUserId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : (_currentUserService.UserId ?? _currentUserService.UserName ?? "system");

        var normalizedOldType = transaction.TransactionType.Trim().ToUpperInvariant();
        var normalizedNewType = request.TransactionType.Trim().ToUpperInvariant();

        // 1. Fetch products
        var oldProduct = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == transaction.ProductId, cancellationToken);
        if (oldProduct == null)
        {
            throw new KeyNotFoundException($"Product with ID {transaction.ProductId} not found.");
        }

        Product newProduct;
        if (transaction.ProductId != request.ProductId)
        {
            newProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.DeletedOn.HasValue, cancellationToken)
                ?? throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");
        }
        else
        {
            newProduct = oldProduct;
        }

        // 2. Fetch WarehouseStocks
        var oldSourceStock = await GetOrCreateWarehouseStockAsync(transaction.ProductId, transaction.WarehouseId, effectiveUserId, cancellationToken);
        WarehouseStocks? oldDestStock = null;
        if (transaction.ToWarehouseId.HasValue && transaction.ToWarehouseId.Value > 0)
        {
            oldDestStock = await GetOrCreateWarehouseStockAsync(transaction.ProductId, transaction.ToWarehouseId.Value, effectiveUserId, cancellationToken);
        }

        var newSourceStock = (transaction.ProductId == request.ProductId && transaction.WarehouseId == request.WarehouseId)
            ? oldSourceStock
            : await GetOrCreateWarehouseStockAsync(request.ProductId, request.WarehouseId, effectiveUserId, cancellationToken);

        WarehouseStocks? newDestStock = null;
        if (request.ToWarehouseId.HasValue && request.ToWarehouseId.Value > 0)
        {
            newDestStock = (transaction.ProductId == request.ProductId && transaction.ToWarehouseId == request.ToWarehouseId)
                ? oldDestStock
                : await GetOrCreateWarehouseStockAsync(request.ProductId, request.ToWarehouseId.Value, effectiveUserId, cancellationToken);
        }

        // 3. Revert old transaction impact on warehouse stocks
        if (normalizedOldType == "IN")
        {
            oldSourceStock.Quantity -= transaction.Quantity;
        }
        else if (normalizedOldType == "OUT")
        {
            oldSourceStock.Quantity += transaction.Quantity;
        }
        else if (normalizedOldType == "TRANSFER")
        {
            oldSourceStock.Quantity += transaction.Quantity;
            if (oldDestStock != null)
            {
                oldDestStock.Quantity -= transaction.Quantity;
            }
        }

        // 4. Apply new transaction impact on warehouse stocks
        if (normalizedNewType == "IN")
        {
            newSourceStock.Quantity += request.Quantity;
        }
        else if (normalizedNewType == "OUT")
        {
            if (newSourceStock.Quantity < request.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock available in selected warehouse for product '{newProduct.Name}'. Available: {newSourceStock.Quantity}, Requested: {request.Quantity}.");
            }
            newSourceStock.Quantity -= request.Quantity;
        }
        else if (normalizedNewType == "TRANSFER")
        {
            if (!request.ToWarehouseId.HasValue || request.ToWarehouseId.Value <= 0)
            {
                throw new InvalidOperationException("Destination Warehouse is required for stock transfer.");
            }
            if (request.WarehouseId == request.ToWarehouseId.Value)
            {
                throw new InvalidOperationException("Source warehouse and Destination warehouse cannot be the same.");
            }
            if (newSourceStock.Quantity < request.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock available in source warehouse for product '{newProduct.Name}'. Available: {newSourceStock.Quantity}, Requested: {request.Quantity}.");
            }
            if (newDestStock == null)
            {
                newDestStock = await GetOrCreateWarehouseStockAsync(request.ProductId, request.ToWarehouseId.Value, effectiveUserId, cancellationToken);
            }

            newSourceStock.Quantity -= request.Quantity;
            newDestStock.Quantity += request.Quantity;
        }
        else if (normalizedNewType == "ADJUSTMENT")
        {
            newSourceStock.Quantity = request.Quantity;
        }

        var isTransfer = string.Equals(normalizedNewType, "TRANSFER", StringComparison.OrdinalIgnoreCase);

        // Update transaction record
        transaction.ProductId = request.ProductId;
        transaction.WarehouseId = request.WarehouseId;
        transaction.WarehouseLocationId = request.WarehouseLocationId;
        transaction.ToWarehouseId = isTransfer ? request.ToWarehouseId : null;
        transaction.ToWarehouseLocationId = isTransfer ? request.ToWarehouseLocationId : null;
        transaction.Quantity = request.Quantity;
        transaction.TransactionType = normalizedNewType;
        transaction.TransactionDate = request.TransactionDate != default 
            ? (request.TransactionDate.Kind == DateTimeKind.Utc ? request.TransactionDate.ToLocalTime() : request.TransactionDate)
            : DateTime.Now;
        transaction.ReferenceNo = request.ReferenceNo;
        transaction.Note = request.Note?.Trim();
        transaction.UpdatedOn = DateTime.Now;
        transaction.UpdatedBy = effectiveUserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Always sync Product.CurrentStock to the sum of all warehouse stocks for that product
        newProduct.CurrentStock = await _context.WarehouseStocks
            .Where(w => w.ProductId == newProduct.Id && !w.DeletedOn.HasValue)
            .SumAsync(w => w.Quantity, cancellationToken);

        if (oldProduct.Id != newProduct.Id)
        {
            oldProduct.CurrentStock = await _context.WarehouseStocks
                .Where(w => w.ProductId == oldProduct.Id && !w.DeletedOn.HasValue)
                .SumAsync(w => w.Quantity, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == transaction.WarehouseId, cancellationToken);
        var location = await _context.WarehouseLocations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == transaction.WarehouseLocationId, cancellationToken);

        Warehouse? toWarehouse = null;
        WarehouseLocation? toLocation = null;
        if (transaction.ToWarehouseId.HasValue)
        {
            toWarehouse = await _context.Warehouses.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == transaction.ToWarehouseId.Value, cancellationToken);
        }
        if (transaction.ToWarehouseLocationId.HasValue)
        {
            toLocation = await _context.WarehouseLocations.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == transaction.ToWarehouseLocationId.Value, cancellationToken);
        }

        return new StockTransactionsDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = newProduct.Name,
            ProductSku = newProduct.Sku,
            UserId = transaction.UserId,
            WarehouseId = transaction.WarehouseId,
            WarehouseName = warehouse?.Name,
            WarehouseLocationId = transaction.WarehouseLocationId,
            WarehouseLocationName = location?.LocationCode,
            ToWarehouseId = transaction.ToWarehouseId,
            ToWarehouseName = toWarehouse?.Name,
            ToWarehouseLocationId = transaction.ToWarehouseLocationId,
            ToWarehouseLocationName = toLocation?.LocationCode,
            Quantity = transaction.Quantity,
            TransactionType = transaction.TransactionType,
            TransactionDate = transaction.TransactionDate,
            ReferenceNo = transaction.ReferenceNo,
            Note = transaction.Note,
            CreatedOn = transaction.CreatedOn,
            CreatedBy = transaction.CreatedBy,
            UpdatedOn = transaction.UpdatedOn,
            UpdatedBy = transaction.UpdatedBy,
            DeletedOn = transaction.DeletedOn,
            DeletedBy = transaction.DeletedBy
        };
    }

    private async Task<WarehouseStocks> GetOrCreateWarehouseStockAsync(Guid productId, int warehouseId, string effectiveUserId, CancellationToken cancellationToken)
    {
        var stock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId && !x.DeletedOn.HasValue, cancellationToken);

        if (stock == null)
        {
            stock = new WarehouseStocks
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                CreatedOn = DateTime.Now,
                CreatedBy = effectiveUserId
            };
            _context.WarehouseStocks.Add(stock);
        }

        return stock;
    }
}
