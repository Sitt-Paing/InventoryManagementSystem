using InventoryManagementSystem.Domain.Enums;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;

public class PurchaseReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public PurchaseReturnStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? ReturnReason { get; set; }
    public string? PostedBy { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? PostedOn { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public int TotalItems => Items.Count;
    public List<PurchaseReturnItemDto> Items { get; set; } = new();
}
