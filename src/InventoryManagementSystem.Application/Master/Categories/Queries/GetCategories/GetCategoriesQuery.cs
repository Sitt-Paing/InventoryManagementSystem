using InventoryManagementSystem.Application.Master.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Categories.Queries.GetCategories;

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;
