using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.DeleteUomCategory;

public record DeleteUomCategoryCommand(long Id) : IRequest<UomCategoryDto?>;
