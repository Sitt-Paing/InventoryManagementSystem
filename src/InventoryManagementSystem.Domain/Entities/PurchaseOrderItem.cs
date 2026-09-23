using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class PurchaseOrderItem : BaseAuditableEntity<long>
{
    public Guid PurchaseOrderId { get; set; }

    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public long UomId { get; set; }
    public decimal ReceivedQuantity { get; set; }

    [JsonIgnore]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;
    [JsonIgnore]
    public virtual UnitOfMeasure Uom { get; set; } = null!;
}