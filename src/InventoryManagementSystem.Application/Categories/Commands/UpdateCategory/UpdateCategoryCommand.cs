using InventoryManagementSystem.Application.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<CategoryDto?>;
