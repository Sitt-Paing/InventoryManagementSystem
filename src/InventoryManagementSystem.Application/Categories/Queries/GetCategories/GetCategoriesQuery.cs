using InventoryManagementSystem.Application.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Categories.Queries.GetCategories;

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;
