using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Warehouses.DTOs;

public record class WarehouseDto
{
    public int Id { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal? Capacity { get; set; }

    public bool Status { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedOn { get; set; }

    public string? DeletedBy { get; set; }

}
