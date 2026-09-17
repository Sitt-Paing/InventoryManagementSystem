using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Warehouses.Queries.GetWarehouseById;

public record class GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto?>;
