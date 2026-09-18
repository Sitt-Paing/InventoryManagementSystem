using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactionById;

public record class GetStockTransactionByIdQuery(long Id) : IRequest<StockTransactionsDto?>;
