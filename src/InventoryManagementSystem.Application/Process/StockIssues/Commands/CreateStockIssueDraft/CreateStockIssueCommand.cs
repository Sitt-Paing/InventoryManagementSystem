using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;

public record CreateStockIssueItemInput(
    Guid ProductId,
    decimal Quantity
);

public record CreateStockIssueCommand(
    string IssueNo,
    DateTime IssueDate,
    int WarehouseId,
    int DepartmentId,
    string ReceivedBy,
    string? Note,
    List<CreateStockIssueItemInput> Items
) : IRequest<StockIssueDto>;
