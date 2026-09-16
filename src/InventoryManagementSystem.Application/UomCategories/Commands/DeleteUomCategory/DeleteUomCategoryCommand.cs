using InventoryManagementSystem.Application.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UomCategories.Commands.DeleteUomCategory;

public record DeleteUomCategoryCommand(long Id) : IRequest<UomCategoryDto?>;
