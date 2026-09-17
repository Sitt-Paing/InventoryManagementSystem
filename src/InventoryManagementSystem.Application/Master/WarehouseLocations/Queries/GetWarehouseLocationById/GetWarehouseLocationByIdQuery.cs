using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Queries.GetWarehouseLocationById;

public record class GetWarehouseLocationByIdQuery(int Id) : IRequest<WarehouseLocationDto?>;
