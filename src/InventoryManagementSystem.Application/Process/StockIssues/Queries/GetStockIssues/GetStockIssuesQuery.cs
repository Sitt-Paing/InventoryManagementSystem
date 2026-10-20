using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockIssues.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;

namespace InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssues;

public class GetStockIssuesQuery : IRequest<PagedResult<StockIssueDto>>
{
    public string? Search { get; set; }
    public StockIssueStatus? Status { get; set; }
    public int? WarehouseId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}
