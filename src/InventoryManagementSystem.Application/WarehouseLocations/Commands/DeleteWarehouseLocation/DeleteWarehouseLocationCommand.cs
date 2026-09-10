using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.WarehouseLocations.Commands.DeleteWarehouseLocation;

public record class DeleteWarehouseLocationCommand(int Id) : IRequest<WarehouseLocationDto?>;
