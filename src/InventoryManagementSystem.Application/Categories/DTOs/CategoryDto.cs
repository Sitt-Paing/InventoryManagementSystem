namespace InventoryManagementSystem.Application.Categories.DTOs;

public record CategoryDto(
    long Id,
    string Name,
    string? Description,
    bool IsActive
);
