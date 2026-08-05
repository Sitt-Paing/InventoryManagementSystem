using InventoryManagementSystem.Application.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
