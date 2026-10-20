using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Department : BaseAuditableEntity<int> , IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
