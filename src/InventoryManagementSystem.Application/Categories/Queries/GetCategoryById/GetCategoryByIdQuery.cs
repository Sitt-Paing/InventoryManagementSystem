using InventoryManagementSystem.Application.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(long Id) : IRequest<CategoryDto?>;
