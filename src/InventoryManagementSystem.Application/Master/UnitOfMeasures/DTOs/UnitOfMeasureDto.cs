using System;

namespace InventoryManagementSystem.Application.Master.UnitOfMeasures.DTOs;

public record UnitOfMeasureDto
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Symbol { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
