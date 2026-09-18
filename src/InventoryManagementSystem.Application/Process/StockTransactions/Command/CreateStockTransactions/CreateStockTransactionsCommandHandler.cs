using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;

public class CreateStockTransactionsCommandHandler : IRequestHandler<CreateStockTransactionsCommand, StockTransactionsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateStockTransactionsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<StockTransactionsDto> Handle(CreateStockTransactionsCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == command.ProductId && !x.DeletedOn.HasValue, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{command.ProductId}' was not found.");
        }

        var normalizedType = command.TransactionType.Trim().ToUpperInvariant();

        var effectiveUserId = !string.IsNullOrWhiteSpace(command.UserId)
            ? command.UserId
            : (_currentUserService.UserId ?? _currentUserService.UserName ?? "system");

        var sourceStock = await _context.WarehouseStocks
            .FirstOrDefaultAsync(x => x.ProductId == command.ProductId && x.WarehouseId == command.WarehouseId && !x.DeletedOn.HasValue, cancellationToken);

        if (sourceStock == null)
        {
            sourceStock = new WarehouseStocks
            {
                ProductId = command.ProductId,
                WarehouseId = command.WarehouseId,
                Quantity = 0,
                CreatedOn = DateTime.Now,
                CreatedBy = effectiveUserId
            };
            _context.WarehouseStocks.Add(sourceStock);
        }

        if (string.Equals(normalizedType, "IN", StringComparison.OrdinalIgnoreCase))
        {
            product.CurrentStock += command.Quantity;
            sourceStock.Quantity += command.Quantity;
        }
        else if (string.Equals(normalizedType, "OUT", StringComparison.OrdinalIgnoreCase))
        {
            if (sourceStock.Quantity < command.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock in the selected warehouse for '{product.Name}'. Available warehouse stock is {sourceStock.Quantity}, but requested quantity is {command.Quantity}.");
            }
            product.CurrentStock -= command.Quantity;
            sourceStock.Quantity -= command.Quantity;
        }
        else if (string.Equals(normalizedType, "ADJUSTMENT", StringComparison.OrdinalIgnoreCase))
        {
            var diff = command.Quantity - sourceStock.Quantity;
            sourceStock.Quantity = command.Quantity;
            product.CurrentStock += diff;
        }
        else if (string.Equals(normalizedType, "TRANSFER", StringComparison.OrdinalIgnoreCase))
        {
            if (!command.ToWarehouseId.HasValue || command.ToWarehouseId.Value <= 0)
            {
                throw new InvalidOperationException("Destination Warehouse is required for stock transfer.");
            }

            if (command.WarehouseId == command.ToWarehouseId.Value)
            {
                throw new InvalidOperationException("Source warehouse and Destination warehouse cannot be the same.");
            }

            if (sourceStock.Quantity < command.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock in the source warehouse for '{product.Name}'. Available source warehouse stock is {sourceStock.Quantity}, but requested transfer quantity is {command.Quantity}.");
            }

            var destStock = await _context.WarehouseStocks
                .FirstOrDefaultAsync(x => x.ProductId == command.ProductId && x.WarehouseId == command.ToWarehouseId.Value && !x.DeletedOn.HasValue, cancellationToken);

            if (destStock == null)
            {
                destStock = new WarehouseStocks
                {
                    ProductId = command.ProductId,
                    WarehouseId = command.ToWarehouseId.Value,
                    Quantity = 0,
                    CreatedOn = DateTime.Now,
                    CreatedBy = effectiveUserId
                };
                _context.WarehouseStocks.Add(destStock);
            }

            sourceStock.Quantity -= command.Quantity;
            destStock.Quantity += command.Quantity;
            // Product.CurrentStock is preserved (net zero change across warehouses)
        }

        var transactionDate = command.TransactionDate != default 
            ? (command.TransactionDate.Kind == DateTimeKind.Utc ? command.TransactionDate.ToLocalTime() : command.TransactionDate)
            : DateTime.Now;

        var isTransfer = string.Equals(normalizedType, "TRANSFER", StringComparison.OrdinalIgnoreCase);

        var transaction = new StockTransaction
        {
            ProductId = command.ProductId,
            UserId = effectiveUserId,
            WarehouseId = command.WarehouseId,
            WarehouseLocationId = command.WarehouseLocationId,
            ToWarehouseId = isTransfer ? command.ToWarehouseId : null,
            ToWarehouseLocationId = isTransfer ? command.ToWarehouseLocationId : null,
            Quantity = command.Quantity,
            TransactionType = normalizedType,
            TransactionDate = transactionDate,
            ReferenceNo = command.ReferenceNo,
            Note = command.Note
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        var warehouse = await _context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == command.WarehouseId, cancellationToken);
        var location = await _context.WarehouseLocations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == command.WarehouseLocationId, cancellationToken);

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
            ProductName = product.Name,
            ProductSku = product.Sku,
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
}
