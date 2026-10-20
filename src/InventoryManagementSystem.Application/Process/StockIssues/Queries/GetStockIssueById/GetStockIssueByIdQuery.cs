using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssueById;

public record GetStockIssueByIdQuery(Guid Id) : IRequest<StockIssueDto>;
