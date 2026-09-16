using InventoryManagementSystem.Application.UomCategories.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.UomCategories.Queries.GetUomCategories;

public record GetUomCategoriesQuery : IRequest<List<UomCategoryDto>>;
