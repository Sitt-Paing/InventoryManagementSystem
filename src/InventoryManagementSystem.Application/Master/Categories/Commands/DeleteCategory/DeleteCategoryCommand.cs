using InventoryManagementSystem.Application.Master.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(long Id) : IRequest<CategoryDto?>;
