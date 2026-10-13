using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseReturn : BaseAuditableEntity<Guid>, IMustHaveCompany 
{ 
    public int? CompanyId { get; set; }
    public int SupplierId { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? ReturnReason { get; set; }
    public DateTime? PostedOn { get; set; }
    public string? PostedBy { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public string? IdempotencyRequestHash { get; set; }
    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }
    [JsonIgnore]
    public virtual ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}


