using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategoryById;

public record GetUomCategoryByIdQuery(long Id) : IRequest<UomCategoryDto?>;
