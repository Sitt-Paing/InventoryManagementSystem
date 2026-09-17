using InventoryManagementSystem.Application.Master.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<CategoryDto?>;
