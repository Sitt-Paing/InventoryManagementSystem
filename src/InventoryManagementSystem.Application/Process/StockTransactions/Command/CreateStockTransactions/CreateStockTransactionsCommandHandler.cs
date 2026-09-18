using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;

public class CreateStockTransactionsCommandHandler : IRequestHandler<CreateStockTransactionsCommand, StockTrasactionsDto>
{
    private readonly IApplicationDbContext _context;

    public CreateStockTransactionsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockTrasactionsDto> Handle(CreateStockTransactionsCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == command.ProductId && !x.DeletedOn.HasValue, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID '{command.ProductId}' was not found.");
        }

        if (string.Equals(command.TransactionType, "IN", StringComparison.OrdinalIgnoreCase))
        {
            product.CurrentStock += command.Quantity;
        }
        else if (string.Equals(command.TransactionType, "OUT", StringComparison.OrdinalIgnoreCase))
        {
            if (product.CurrentStock < command.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. Current stock is {product.CurrentStock}, but requested quantity is {command.Quantity}.");
            }
            product.CurrentStock -= command.Quantity;
        }
        else if (string.Equals(command.TransactionType, "ADJUSTMENT", StringComparison.OrdinalIgnoreCase))
        {
            product.CurrentStock = command.Quantity;
        }

        var transaction = new StockTransaction
        {
            ProductId = command.ProductId,
            UserId = command.UserId,
            WarehouseId = command.WarehouseId,
            WarehouseLocationId = command.WarehouseLocationId,
            Quantity = command.Quantity,
            TransactionType = command.TransactionType.ToUpperInvariant(),
            TransactionDate = DateTime.UtcNow,
            ReferenceNo = command.ReferenceNo,
            Note = command.Note
        };

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new StockTrasactionsDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = product.Name,
            ProductSku = product.Sku,
            UserId = transaction.UserId,
            WarehouseId = transaction.WarehouseId,
            WarehouseLocationId = transaction.WarehouseLocationId,
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
