using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Supplier : BaseAuditableEntity<int>
{
    public string SupplierCode { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string PaymentTerms { get; set; } = null!;

    public decimal? CreditLimit { get; set; }

    public bool Status { get; set; }
}
