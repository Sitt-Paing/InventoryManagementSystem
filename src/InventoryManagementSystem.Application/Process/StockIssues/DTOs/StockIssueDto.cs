using InventoryManagementSystem.Domain.Enums;

namespace InventoryManagementSystem.Application.Process.StockIssues.DTOs;

public class StockIssueDto
{
    public Guid Id { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string? Note { get; set; }
    public StockIssueStatus Status { get; set; }
    public string? PostedBy { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? PostedOn { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public int TotalItems => Items.Count;
    public List<StockIssueItemDto> Items { get; set; } = new();
}
