using InventoryManagementSystem.Application.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UomCategories.Commands.CreateUomCategory;

public record CreateUomCategoryCommand(string Name, string? Description) : IRequest<UomCategoryDto>;
