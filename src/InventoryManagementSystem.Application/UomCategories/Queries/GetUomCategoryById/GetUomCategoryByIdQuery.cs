using InventoryManagementSystem.Application.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UomCategories.Queries.GetUomCategoryById;

public record GetUomCategoryByIdQuery(long Id) : IRequest<UomCategoryDto?>;
