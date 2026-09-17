using InventoryManagementSystem.Application.Master.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(string Name, string? Description) : IRequest<CategoryDto>;
