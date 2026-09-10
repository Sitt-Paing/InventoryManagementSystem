using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.WarehouseLocations.Queries.GetWarehouseLocations;

public record class GetWarehouseLocationsQuery(int? WarehouseId = null) : IRequest<List<WarehouseLocationDto>>;
