using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public partial class Company : BaseAuditableEntity<int>
{
    public string CompanyName { get; set; } = string.Empty!;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    [JsonIgnore]
    public virtual ICollection<AspNetUser> AspNetUsers { get; set; } = new List<AspNetUser>();
}
