using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseReturnItem : BaseAuditableEntity<long>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public Guid PurchaseReturnId { get; set; }
    public long GoodReceiptItemId { get; set; }
    public decimal ReturnQuantity { get; set; } = 0;
    [JsonIgnore]
    public virtual PurchaseReturn? PurchaseReturn { get; set; }
    [JsonIgnore]
    public virtual GoodReceiptItem? GoodReceiptItem { get; set; }
}
