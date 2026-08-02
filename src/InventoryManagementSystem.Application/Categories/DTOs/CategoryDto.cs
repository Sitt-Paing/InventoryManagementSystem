namespace InventoryManagementSystem.Application.Categories.DTOs;

public record CategoryDto
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
