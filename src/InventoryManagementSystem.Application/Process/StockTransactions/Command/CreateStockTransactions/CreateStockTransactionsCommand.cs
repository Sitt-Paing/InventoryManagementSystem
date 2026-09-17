using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;

public record class CreateStockTransactionsCommand(long Id, string ProductId,string UserId, int WarehouseId, int WarehouseLocationId, decimal Quantity, string TransactionType, DateTime TransactionDate, long? ReferenceNo, string? Note) : IRequest<StockTrasactionsDto>;