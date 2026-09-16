using InventoryManagementSystem.Application.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.UomCategories.Commands.UpdateUomCategory;

public record UpdateUomCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<UomCategoryDto?>;
