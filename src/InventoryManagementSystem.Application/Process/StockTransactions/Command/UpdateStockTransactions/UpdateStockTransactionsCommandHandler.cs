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

        var normalizedNewType = request.TransactionType.Trim().ToUpperInvariant();
        Product activeProduct;

        // Check if product changed
        if (transaction.ProductId != request.ProductId)
        {
            // Revert old product stock
            var oldProduct = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == transaction.ProductId, cancellationToken);
            if (oldProduct != null)
            {
                if (transaction.TransactionType == "IN")
                {
                    oldProduct.CurrentStock -= transaction.Quantity;
                }
                else if (transaction.TransactionType == "OUT")
                {
                    oldProduct.CurrentStock += transaction.Quantity;
                }
            }

            // Apply new product stock
            var newProduct = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.DeletedOn.HasValue, cancellationToken);
            if (newProduct == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");
            }

            if (normalizedNewType == "IN")
            {
                newProduct.CurrentStock += request.Quantity;
            }
            else if (normalizedNewType == "OUT")
            {
                if (newProduct.CurrentStock < request.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock available for product '{newProduct.Name}'. Current stock: {newProduct.CurrentStock}, Requested: {request.Quantity}.");
                }
                newProduct.CurrentStock -= request.Quantity;
            }
            else if (normalizedNewType == "ADJUSTMENT")
            {
                newProduct.CurrentStock = request.Quantity;
            }

            activeProduct = newProduct;
        }
        else
        {
            // Same product: revert old transaction impact, then apply new transaction impact
            var product = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");
            }

            // Step 1: Revert previous movement
            if (transaction.TransactionType == "IN")
            {
                product.CurrentStock -= transaction.Quantity;
            }
            else if (transaction.TransactionType == "OUT")
            {
                product.CurrentStock += transaction.Quantity;
            }

            // Step 2: Apply updated movement
            if (normalizedNewType == "IN")
            {
                product.CurrentStock += request.Quantity;
            }
            else if (normalizedNewType == "OUT")
            {
                if (product.CurrentStock < request.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock available for product '{product.Name}'. Available stock after reverting previous movement: {product.CurrentStock}, Requested: {request.Quantity}.");
                }
                product.CurrentStock -= request.Quantity;
            }
            else if (normalizedNewType == "ADJUSTMENT")
            {
                product.CurrentStock = request.Quantity;
            }

            activeProduct = product;
        }

        var effectiveUserId = !string.IsNullOrWhiteSpace(request.UserId)
            ? request.UserId
            : (_currentUserService.UserId ?? _currentUserService.UserName ?? "system");

        // Update transaction entity properties
        transaction.ProductId = request.ProductId;
        transaction.WarehouseId = request.WarehouseId;
        transaction.WarehouseLocationId = request.WarehouseLocationId;
        transaction.Quantity = request.Quantity;
        transaction.TransactionType = normalizedNewType;
        transaction.TransactionDate = request.TransactionDate != default ? request.TransactionDate : DateTime.UtcNow;
        transaction.ReferenceNo = request.ReferenceNo;
        transaction.Note = request.Note?.Trim();
        transaction.UpdatedOn = DateTime.UtcNow;
        transaction.UpdatedBy = effectiveUserId;

        await _context.SaveChangesAsync(cancellationToken);

        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == transaction.WarehouseId, cancellationToken);
        var location = await _context.WarehouseLocations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == transaction.WarehouseLocationId, cancellationToken);

        return new StockTransactionsDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = activeProduct.Name,
            ProductSku = activeProduct.Sku,
            UserId = transaction.UserId,
            WarehouseId = transaction.WarehouseId,
            WarehouseName = warehouse?.Name,
            WarehouseLocationId = transaction.WarehouseLocationId,
            WarehouseLocationName = location?.LocationCode,
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
}
