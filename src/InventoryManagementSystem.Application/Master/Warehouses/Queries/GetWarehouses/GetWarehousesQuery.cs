using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using MediatR;
using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Master.Warehouses.Queries.GetWarehouses;

public record class GetWarehousesQuery : IRequest<List<WarehouseDto>>;
