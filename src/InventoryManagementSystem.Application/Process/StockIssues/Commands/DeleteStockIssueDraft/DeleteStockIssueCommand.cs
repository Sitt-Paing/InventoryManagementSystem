using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.DeleteStockIssueDraft;

public record DeleteStockIssueCommand(Guid Id) : IRequest;
