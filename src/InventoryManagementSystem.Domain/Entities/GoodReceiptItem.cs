using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class GoodReceiptItem : BaseAuditableEntity<long>
{
    public Guid GoodReceiptId { get; set; }
    public long PurchaseOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public long UomId { get; set; }
    public decimal ReceivedQuantity { get; set; }

    [JsonIgnore]
    public virtual GoodReceipt GoodReceipt { get; set; } = null!;

    [JsonIgnore]
    public virtual PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;

    [JsonIgnore]
    public virtual UnitOfMeasure Uom { get; set; } = null!;
}