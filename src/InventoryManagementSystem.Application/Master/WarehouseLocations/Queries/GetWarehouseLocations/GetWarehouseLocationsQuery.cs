using InventoryManagementSystem.Application.Master.WarehouseLocations.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.WarehouseLocations.Queries.GetWarehouseLocations;

public record class GetWarehouseLocationsQuery(int? WarehouseId = null) : IRequest<List<WarehouseLocationDto>>;
