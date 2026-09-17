using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategories;

public record GetUomCategoriesQuery : IRequest<List<UomCategoryDto>>;
