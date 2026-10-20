using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssuePost;

public record CreateStockIssuePostCommand(Guid StockIssueId) : IRequest<StockIssueDto>;
