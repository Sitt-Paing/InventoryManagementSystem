using InventoryManagementSystem.Application.Warehouses.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Warehouses.Queries.GetWarehouses;

public record class GetWarehousesQuery : IRequest<List<WarehouseDto>>;
