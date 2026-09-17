using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.CreateUomCategory;

public record CreateUomCategoryCommand(string Name, string? Description) : IRequest<UomCategoryDto>;
