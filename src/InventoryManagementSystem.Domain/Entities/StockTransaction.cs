using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public partial class StockTransaction : BaseAuditableEntity<long>
{
    public string ProductId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public int Quantity { get; set; }

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public string? Note { get; set; }

    [JsonIgnore]
    public virtual Product Product { get; set; } = null!;
}
