using InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.UpdateStockIssueDraft;

public record UpdateStockIssueCommand(
    Guid Id,
    DateTime IssueDate,
    int WarehouseId,
    int DepartmentId,
    string ReceivedBy,
    string? Note,
    List<CreateStockIssueItemInput> Items
) : IRequest<StockIssueDto>;
