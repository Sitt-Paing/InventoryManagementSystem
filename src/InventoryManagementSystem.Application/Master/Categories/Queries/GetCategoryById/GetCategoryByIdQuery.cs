using InventoryManagementSystem.Application.Master.Categories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(long Id) : IRequest<CategoryDto?>;
