using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.WarehouseLocations.Queries.GetWarehouseLocationById;

public record class GetWarehouseLocationByIdQuery(int Id) : IRequest<WarehouseLocationDto?>;
