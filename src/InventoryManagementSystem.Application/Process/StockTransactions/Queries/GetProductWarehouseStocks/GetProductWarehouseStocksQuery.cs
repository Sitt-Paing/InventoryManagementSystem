using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetProductWarehouseStocks;

public record class GetProductWarehouseStocksQuery(Guid ProductId) : IRequest<List<ProductWarehouseStockDto>>;
