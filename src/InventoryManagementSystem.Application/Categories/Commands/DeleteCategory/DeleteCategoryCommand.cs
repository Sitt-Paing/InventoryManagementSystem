using InventoryManagementSystem.Application.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(long Id) : IRequest<CategoryDto?>;
