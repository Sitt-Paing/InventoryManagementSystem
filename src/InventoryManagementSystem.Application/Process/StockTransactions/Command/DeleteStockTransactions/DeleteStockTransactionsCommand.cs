using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockTransactions.Command.DeleteStockTransactions;

public record class DeleteStockTransactionsCommand(long Id, string? UserId = null) : IRequest<StockTransactionsDto?>;
