using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.UpdateUomCategory;

public record UpdateUomCategoryCommand(long Id, string Name, string? Description, bool IsActive) : IRequest<UomCategoryDto?>;
