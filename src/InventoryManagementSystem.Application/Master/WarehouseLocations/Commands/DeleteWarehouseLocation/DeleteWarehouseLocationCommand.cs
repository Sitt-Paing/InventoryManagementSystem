using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Commands.DeleteWarehouseLocation;

public record class DeleteWarehouseLocationCommand(int Id) : IRequest<WarehouseLocationDto?>;
